#!/usr/bin/env python3
"""
GitHub PR AI code reviewer.

MVP behavior:
- Runs in GitHub Actions on push, pull_request, or workflow_dispatch.
- Fetches PR or pushed commit metadata and changed files through GitHub REST API.
- Applies deterministic blocking/major/minor review rules.
- Optionally asks an AI model for semantic review when OPENAI_API_KEY is set.
- Posts a PR review summary when a PR exists; otherwise writes the Actions summary.
- Exits non-zero when blocking findings or too many major findings are present.
"""

from __future__ import annotations

import fnmatch
import json
import os
import re
import sys
import textwrap
import urllib.error
import urllib.request
from dataclasses import dataclass
from pathlib import Path
from typing import Any


DEFAULT_CONFIG = {
    "review": {
        "fail_on": ["blocking"],
        "max_major_before_fail": 3,
    },
    "limits": {
        "max_patch_chars": 60000,
        "max_file_patch_chars": 12000,
    },
    "paths": {
        "ignore": [
            ".git/**",
            "Library/**",
            "Temp/**",
            "Logs/**",
            "obj/**",
            "UserSettings/**",
            "Assets/YooAsset/**",
            "Assets/Character Controller Pro/**",
            "Assets/KinematicCharacterController/**",
            "*.generated.cs",
            "*.meta",
        ],
        "require_tests_for": ["server/src/**", "Assets/Script/**"],
    },
    "style": {
        "language": "zh-CN",
        "strictness": "medium",
    },
}


SECRET_PATTERNS = [
    re.compile(r"(?i)(api[_-]?key|secret|token|password|passwd|pwd)\s*[:=]\s*['\"][^'\"]{8,}['\"]"),
    re.compile(r"-----BEGIN (RSA |EC |OPENSSH |DSA )?PRIVATE KEY-----"),
    re.compile(r"ghp_[A-Za-z0-9_]{30,}"),
    re.compile(r"github_pat_[A-Za-z0-9_]{40,}"),
    re.compile(r"sk-[A-Za-z0-9_-]{20,}"),
]


DANGEROUS_PATTERNS = [
    ("blocking", "shell", re.compile(r"\b(system|popen|ShellExecute|CreateProcess)\s*\("), "新增了可能执行系统命令的调用，需要确认输入是否可控并避免命令注入。"),
    ("blocking", "eval", re.compile(r"\b(eval|exec)\s*\("), "新增了动态执行代码的调用，默认应视为高风险。"),
    ("major", "sql", re.compile(r"(?i)(SELECT|INSERT|UPDATE|DELETE).*(\+|\$|String\.Format|\{0\})"), "疑似拼接 SQL，请优先使用参数化查询。"),
    ("major", "unity-find", re.compile(r"\b(GameObject\.Find|FindObjectOfType|FindObjectsOfType)\s*<*\w*"), "Unity 运行时频繁 Find 容易造成性能和耦合问题，请确认不在高频路径。"),
]


@dataclass
class Finding:
    severity: str
    category: str
    file: str
    line: int | None
    title: str
    body: str
    suggestion: str = ""
    confidence: float = 1.0
    source: str = "rule"


class GitHubClient:
    def __init__(self, token: str, repository: str, api_url: str = "https://api.github.com") -> None:
        self.token = token
        self.repository = repository
        self.api_url = api_url.rstrip("/")

    def request(self, method: str, path: str, body: dict[str, Any] | None = None, accept: str = "application/vnd.github+json") -> Any:
        data = None
        if body is not None:
            data = json.dumps(body).encode("utf-8")
        req = urllib.request.Request(
            f"{self.api_url}{path}",
            data=data,
            method=method,
            headers={
                "Accept": accept,
                "Authorization": f"Bearer {self.token}",
                "X-GitHub-Api-Version": "2022-11-28",
                "Content-Type": "application/json",
                "User-Agent": "fpsresearch-ai-code-reviewer",
            },
        )
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                raw = resp.read().decode("utf-8")
                return json.loads(raw) if raw else None
        except urllib.error.HTTPError as exc:
            message = exc.read().decode("utf-8", errors="replace")
            raise RuntimeError(f"GitHub API {method} {path} failed: {exc.code} {message}") from exc

    def get_pr(self, pr_number: int) -> dict[str, Any]:
        return self.request("GET", f"/repos/{self.repository}/pulls/{pr_number}")

    def get_pr_files(self, pr_number: int) -> list[dict[str, Any]]:
        files: list[dict[str, Any]] = []
        page = 1
        while True:
            batch = self.request("GET", f"/repos/{self.repository}/pulls/{pr_number}/files?per_page=100&page={page}")
            if not batch:
                return files
            files.extend(batch)
            if len(batch) < 100:
                return files
            page += 1

    def compare_commits(self, before_sha: str, after_sha: str) -> dict[str, Any]:
        return self.request("GET", f"/repos/{self.repository}/compare/{before_sha}...{after_sha}")

    def create_review(self, pr_number: int, body: str, event: str) -> None:
        self.request(
            "POST",
            f"/repos/{self.repository}/pulls/{pr_number}/reviews",
            {
                "body": body,
                "event": event,
            },
        )


@dataclass
class ReviewContext:
    mode: str
    pr_number: int | None
    title: str
    author: str
    base_ref: str
    head_ref: str
    before_sha: str | None = None
    after_sha: str | None = None


def load_config(path: Path) -> dict[str, Any]:
    config = json.loads(json.dumps(DEFAULT_CONFIG))
    if not path.exists():
        return config

    current_section: str | None = None
    current_key: str | None = None
    for raw_line in path.read_text(encoding="utf-8").splitlines():
        line = raw_line.rstrip()
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        if not line.startswith(" ") and stripped.endswith(":"):
            current_section = stripped[:-1]
            current_key = None
            config.setdefault(current_section, {})
            continue
        if current_section is None:
            continue
        if line.startswith("  ") and not line.startswith("    ") and ":" in stripped:
            key, value = stripped.split(":", 1)
            current_key = key.strip()
            value = value.strip().strip("\"'")
            config.setdefault(current_section, {})
            if value:
                if value.isdigit():
                    config[current_section][current_key] = int(value)
                else:
                    config[current_section][current_key] = value
            else:
                config[current_section].setdefault(current_key, [])
            continue
        if line.startswith("    - ") and current_key:
            item = stripped[2:].strip().strip("\"'")
            config[current_section].setdefault(current_key, [])
            config[current_section][current_key].append(item)
    return config


def load_event() -> dict[str, Any]:
    event_path = os.getenv("GITHUB_EVENT_PATH")
    if not event_path:
        return {}
    return json.loads(Path(event_path).read_text(encoding="utf-8"))


def get_review_context() -> ReviewContext:
    if os.getenv("PR_NUMBER"):
        return ReviewContext(
            mode="pr",
            pr_number=int(os.environ["PR_NUMBER"]),
            title=f"PR #{os.environ['PR_NUMBER']}",
            author="",
            base_ref="",
            head_ref="",
        )

    event = load_event()
    if "pull_request" in event:
        pr = event["pull_request"]
        return ReviewContext(
            mode="pr",
            pr_number=int(pr["number"]),
            title=pr.get("title", ""),
            author=pr.get("user", {}).get("login", ""),
            base_ref=pr.get("base", {}).get("ref", ""),
            head_ref=pr.get("head", {}).get("ref", ""),
        )

    if os.getenv("GITHUB_EVENT_NAME") == "push" or {"before", "after"}.issubset(event.keys()):
        return ReviewContext(
            mode="push",
            pr_number=None,
            title=f"Push to {event.get('ref', '')}",
            author=event.get("pusher", {}).get("name", ""),
            base_ref=event.get("ref", ""),
            head_ref=event.get("after", ""),
            before_sha=event.get("before"),
            after_sha=event.get("after"),
        )

    raise RuntimeError("This workflow needs a pull_request event, push event, or workflow_dispatch pr_number.")


def matches_any(path: str, patterns: list[str]) -> bool:
    normalized = path.replace("\\", "/")
    return any(fnmatch.fnmatch(normalized, pattern) for pattern in patterns)


def added_lines_from_patch(patch: str) -> list[tuple[int | None, str]]:
    result: list[tuple[int | None, str]] = []
    new_line = None
    for line in patch.splitlines():
        if line.startswith("@@"):
            match = re.search(r"\+(\d+)", line)
            new_line = int(match.group(1)) if match else None
            continue
        if line.startswith("+") and not line.startswith("+++"):
            result.append((new_line, line[1:]))
            if new_line is not None:
                new_line += 1
        elif not line.startswith("-") and new_line is not None:
            new_line += 1
    return result


def rule_review(files: list[dict[str, Any]], config: dict[str, Any]) -> list[Finding]:
    findings: list[Finding] = []
    require_tests_for = config["paths"].get("require_tests_for", [])
    changed_paths = [file["filename"] for file in files]
    has_test_change = any(re.search(r"(?i)(test|tests|spec|editor).*", path) for path in changed_paths)

    for file in files:
        path = file["filename"]
        patch = file.get("patch") or ""
        additions = file.get("additions", 0)
        deletions = file.get("deletions", 0)

        if not patch:
            continue

        if additions + deletions > 800:
            findings.append(Finding(
                severity="major",
                category="reviewability",
                file=path,
                line=None,
                title="单个文件 diff 过大",
                body=f"该文件变更 {additions + deletions} 行，AI 和人工 review 都容易漏问题。建议拆分 PR 或拆分提交。",
                suggestion="将机械改动、资源改动和逻辑改动拆开提交。",
            ))

        for line_number, text in added_lines_from_patch(patch):
            for pattern in SECRET_PATTERNS:
                if pattern.search(text):
                    findings.append(Finding(
                        severity="blocking",
                        category="security",
                        file=path,
                        line=line_number,
                        title="疑似提交了密钥或敏感凭据",
                        body="新增代码中出现疑似 token、password、private key 或 API key。凭据一旦进入 Git 历史需要立即轮换。",
                        suggestion="移除凭据，改用 GitHub Secrets、环境变量或本地配置文件，并轮换已泄露密钥。",
                    ))
                    break

            for severity, category, pattern, message in DANGEROUS_PATTERNS:
                if pattern.search(text):
                    findings.append(Finding(
                        severity=severity,
                        category=category,
                        file=path,
                        line=line_number,
                        title="新增高风险调用",
                        body=message,
                        suggestion="补充输入校验、封装安全 API，或解释该调用为什么不会被外部输入影响。",
                    ))

    if not has_test_change:
        risky_paths = [path for path in changed_paths if matches_any(path, require_tests_for)]
        if risky_paths:
            findings.append(Finding(
                severity="major",
                category="test",
                file=risky_paths[0],
                line=None,
                title="高风险代码变更缺少测试同步更新",
                body="本 PR 修改了核心服务端或 gameplay 脚本，但没有看到测试、验证脚本或测试说明变更。",
                suggestion="补充自动化测试，或在 PR 描述中说明手动验证步骤和覆盖范围。",
            ))

    return findings


def build_ai_prompt(context: ReviewContext, files: list[dict[str, Any]], config: dict[str, Any]) -> str:
    max_patch_chars = int(config["limits"].get("max_patch_chars", 60000))
    max_file_patch_chars = int(config["limits"].get("max_file_patch_chars", 12000))
    chunks: list[str] = []

    for file in files:
        patch = file.get("patch") or ""
        if not patch:
            continue
        if len(patch) > max_file_patch_chars:
            patch = patch[:max_file_patch_chars] + "\n...[patch truncated]..."
        chunks.append(f"### {file['filename']} ({file.get('status')})\n```diff\n{patch}\n```")

    diff_text = "\n\n".join(chunks)
    if len(diff_text) > max_patch_chars:
        diff_text = diff_text[:max_patch_chars] + "\n\n...[diff truncated]..."

    return textwrap.dedent(f"""
    你是一个严格但务实的 AI Code Reviewer。请只审查本 PR diff，不要臆测没有出现的代码。

    请从这些角度审查：
    - correctness: 逻辑正确性、边界条件、崩溃风险
    - security: 注入、凭据、权限、反序列化、命令执行等安全风险
    - maintainability: 命名、复杂度、重复、模块边界
    - architecture: 职责边界、长期演进风险
    - performance: Unity/C++/服务端高频路径性能问题
    - testability: 是否缺少测试或验证入口
    - style: 代码审美，但只有影响维护时才提高严重级别

    严重级别只能是 blocking、major、minor、nit。
    blocking 只用于必须打回的问题，比如安全漏洞、确定性崩溃、数据损坏、破坏关键协议或明显不可合并。

    输出必须是 JSON，不要包 markdown：
    {{
      "summary": "一句话总结",
      "findings": [
        {{
          "severity": "major",
          "category": "correctness",
          "file": "path/to/file.cs",
          "line": 12,
          "title": "短标题",
          "body": "为什么这是问题",
          "suggestion": "如何修复",
          "confidence": 0.8
        }}
      ]
    }}

    Review target:
    - mode: {context.mode}
    - title: {context.title}
    - author: {context.author}
    - base: {context.base_ref}
    - head: {context.head_ref}

    Diff:
    {diff_text}
    """).strip()


def call_ai(context: ReviewContext, files: list[dict[str, Any]], config: dict[str, Any]) -> tuple[str, list[Finding]]:
    api_key = os.getenv("OPENAI_API_KEY")
    if not api_key:
        return "未配置 OPENAI_API_KEY，本次只执行确定性规则审查。", []

    model = os.getenv("AI_REVIEW_MODEL", "gpt-4o-mini")
    prompt = build_ai_prompt(context, files, config)
    payload = {
        "model": model,
        "temperature": 0.1,
        "response_format": {"type": "json_object"},
        "messages": [
            {"role": "system", "content": "You are a senior code reviewer. Return strict JSON only."},
            {"role": "user", "content": prompt},
        ],
    }
    base_url = os.getenv("AI_REVIEW_BASE_URL", "https://api.openai.com/v1").rstrip("/")
    req = urllib.request.Request(
        f"{base_url}/chat/completions",
        data=json.dumps(payload).encode("utf-8"),
        method="POST",
        headers={
            "Authorization": f"Bearer {api_key}",
            "Content-Type": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as exc:
        message = exc.read().decode("utf-8", errors="replace")
        return f"AI 审查调用失败：{exc.code} {message[:500]}", []
    except (TimeoutError, urllib.error.URLError, OSError) as exc:
        return f"AI 审查暂时不可用，本次仅保留确定性规则结果：{exc}", []

    content = data["choices"][0]["message"]["content"]
    try:
        parsed = json.loads(content)
    except json.JSONDecodeError:
        return "AI 返回内容不是合法 JSON，本次忽略 AI 结果。", []

    findings: list[Finding] = []
    for item in parsed.get("findings", []):
        severity = str(item.get("severity", "minor")).lower()
        if severity not in {"blocking", "major", "minor", "nit"}:
            severity = "minor"
        findings.append(Finding(
            severity=severity,
            category=str(item.get("category", "ai")),
            file=str(item.get("file", "")),
            line=item.get("line") if isinstance(item.get("line"), int) else None,
            title=str(item.get("title", "AI finding")),
            body=str(item.get("body", "")),
            suggestion=str(item.get("suggestion", "")),
            confidence=float(item.get("confidence", 0.7)),
            source="ai",
        ))

    return str(parsed.get("summary", "AI 审查完成。")), findings


def deduplicate(findings: list[Finding]) -> list[Finding]:
    seen: set[tuple[str, str, int | None, str]] = set()
    result: list[Finding] = []
    for finding in findings:
        key = (finding.severity, finding.file, finding.line, finding.title)
        if key in seen:
            continue
        seen.add(key)
        result.append(finding)
    return result


def should_fail(findings: list[Finding], config: dict[str, Any]) -> bool:
    fail_on = set(config["review"].get("fail_on", ["blocking"]))
    max_major = int(config["review"].get("max_major_before_fail", 3))
    counts = count_by_severity(findings)
    return any(counts.get(level, 0) > 0 for level in fail_on) or counts.get("major", 0) >= max_major


def count_by_severity(findings: list[Finding]) -> dict[str, int]:
    counts = {"blocking": 0, "major": 0, "minor": 0, "nit": 0}
    for finding in findings:
        counts[finding.severity] = counts.get(finding.severity, 0) + 1
    return counts


def format_review_body(context: ReviewContext, ai_summary: str, findings: list[Finding], ignored_count: int) -> str:
    counts = count_by_severity(findings)
    verdict = "REQUEST_CHANGES" if counts["blocking"] or counts["major"] >= 3 else "COMMENT"
    target_text = f"PR #{context.pr_number}" if context.mode == "pr" else f"push `{context.after_sha or context.head_ref}`"
    lines = [
        "## AI Code Review",
        "",
        f"目标：{target_text}",
        "",
        f"结论：`{verdict}`",
        "",
        f"- Blocking: {counts['blocking']}",
        f"- Major: {counts['major']}",
        f"- Minor: {counts['minor']}",
        f"- Nit: {counts['nit']}",
        f"- Ignored files: {ignored_count}",
        "",
        f"AI summary: {ai_summary}",
    ]

    if not findings:
        lines.extend(["", "没有发现需要阻塞合并的问题。"])
        return "\n".join(lines)

    severity_order = {"blocking": 0, "major": 1, "minor": 2, "nit": 3}
    sorted_findings = sorted(findings, key=lambda item: (severity_order.get(item.severity, 99), item.file, item.line or 0))
    for finding in sorted_findings[:30]:
        location = finding.file
        if finding.line:
            location += f":{finding.line}"
        lines.extend([
            "",
            f"### [{finding.severity.upper()}] {finding.title}",
            "",
            f"- Category: `{finding.category}`",
            f"- Source: `{finding.source}`",
            f"- Location: `{location}`",
            f"- Confidence: `{finding.confidence:.2f}`",
            "",
            finding.body,
        ])
        if finding.suggestion:
            lines.extend(["", f"建议：{finding.suggestion}"])

    if len(sorted_findings) > 30:
        lines.extend(["", f"...另有 {len(sorted_findings) - 30} 条发现未展开。"])

    lines.extend([
        "",
        "---",
        "Generated by `.github/scripts/ai_code_reviewer.py`.",
    ])
    return "\n".join(lines)


def write_step_summary(body: str) -> None:
    summary_path = os.getenv("GITHUB_STEP_SUMMARY")
    if summary_path:
        Path(summary_path).write_text(body, encoding="utf-8")


def main() -> int:
    repository = os.getenv("GITHUB_REPOSITORY")
    token = os.getenv("GITHUB_TOKEN")
    if not repository or not token:
        print("GITHUB_REPOSITORY and GITHUB_TOKEN are required.", file=sys.stderr)
        return 2

    config = load_config(Path(".ai-review.yml"))
    context = get_review_context()
    client = GitHubClient(token, repository, os.getenv("GITHUB_API_URL", "https://api.github.com"))

    if context.mode == "pr":
        if context.pr_number is None:
            raise RuntimeError("PR mode requires pr_number.")
        pr = client.get_pr(context.pr_number)
        context.title = pr.get("title", context.title)
        context.author = pr.get("user", {}).get("login", context.author)
        context.base_ref = pr.get("base", {}).get("ref", context.base_ref)
        context.head_ref = pr.get("head", {}).get("ref", context.head_ref)
        raw_files = client.get_pr_files(context.pr_number)
    else:
        if not context.before_sha or not context.after_sha:
            raise RuntimeError("Push mode requires before and after sha.")
        if set(context.before_sha) == {"0"}:
            raw_files = []
        else:
            comparison = client.compare_commits(context.before_sha, context.after_sha)
            raw_files = comparison.get("files", [])

    ignore_patterns = config["paths"].get("ignore", [])
    files = [file for file in raw_files if not matches_any(file["filename"], ignore_patterns)]
    ignored_count = len(raw_files) - len(files)

    rule_findings = rule_review(files, config)
    ai_summary, ai_findings = call_ai(context, files, config)
    findings = deduplicate(rule_findings + ai_findings)

    body = format_review_body(context, ai_summary, findings, ignored_count)
    write_step_summary(body)

    event = "REQUEST_CHANGES" if should_fail(findings, config) else "COMMENT"
    if context.mode == "pr":
        if context.pr_number is None:
            raise RuntimeError("PR mode requires pr_number.")
        client.create_review(context.pr_number, body, event)
    else:
        print(body)

    if event == "REQUEST_CHANGES":
        print("AI code review requested changes.")
        return 1

    print("AI code review completed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
