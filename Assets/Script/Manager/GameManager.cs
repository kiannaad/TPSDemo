using System;
using UnityEngine;
using System.Collections.Generic;

namespace CGame
{
    public class GameManager : MonoSingleton<GameManager>
    {
        private static readonly LinkedList<IManager> managerList = new LinkedList<IManager>();
  

        public static IManager CreateManager(Type managerType)
        {
            // check Manager exsist
            foreach (IManager m in managerList)
            {
                if (m.GetType() == managerType)
                {
                    return m;
                }
            }
            
            IManager manager = (IManager)Activator.CreateInstance(managerType);
#if DEVELOPMENT_BUILD
            if (manager == null)
            {
                throw new Exception($"Can't Create {managerType}");
            }
#endif
            LinkedListNode<IManager> current = managerList.First;
            while (current != null)
            {
                if (manager.Priority > current.Value.Priority)
                {
                    break;
                }

                current = current.Next;
            }

            if (current != null)
            {
                managerList.AddBefore(current, manager);
            }
            else
            {
                managerList.AddLast(manager);
            }
            manager.Init();
            return manager;
        }

        public static T GetManager<T>() where T : class
        {
            Type interfaceType = typeof(T);
            return GetManager(interfaceType) as T;
        }

        public static void DestoryManager(Type managerType)
        {
            foreach (IManager manager in managerList)
            {
                if (manager.GetType() == managerType)
                {
                    managerList.Remove(manager);
                    manager.Shutdown();
                    return;
                }
            }
        }


        private static IManager GetManager(Type managerType)
        {
            foreach (IManager manager in managerList)
            {
                if (manager.GetType() == managerType)
                {
                    return manager;
                }
            }

            return CreateManager(managerType);
        }
        
        void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;
            foreach (IManager manager in managerList)
            {
                manager.FixedUpdate(deltaTime);
            }
        }

        void Update()
        {
            float deltaTime = Time.deltaTime;
            foreach (IManager manager in managerList)
            {
                manager.Update(deltaTime);
            }
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            foreach (IManager manager in managerList)
            {
                manager.LateUpdate(deltaTime);
            }
        }

        public void OnDestroy()
        {
            LinkedListNode<IManager> currentNode = managerList.Last;
            while (currentNode != null)
            {
                currentNode.Value.Shutdown();
                currentNode = currentNode.Previous;
            }

            managerList.Clear();
        }
    }
}
