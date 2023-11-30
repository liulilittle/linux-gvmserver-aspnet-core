namespace GVMServer.Collection
{
    using System;
    using System.Collections.Generic;

    public class LinkedListIterator<T>
    {
        private LinkedListNode<T> m_poCurrent; // 当前节点
        private LinkedList<T> m_poLinkedList; // 链首指针
        private object m_poCP; // 临界点

        private bool MoveNext()
        {
            lock (m_poCP)
            {
                if (m_poCurrent != null)
                {
                    m_poCurrent = m_poCurrent.Next;
                }

                if (m_poCurrent == null)
                {
                    m_poCurrent = m_poLinkedList.First;
                }
                return m_poCurrent != null;
            }
        }

        private bool MovePrevious()
        {
            lock (m_poCP)
            {
                if (m_poCurrent == null)
                {
                    m_poCurrent = m_poLinkedList.Last;
                }
                else
                {
                    m_poCurrent = m_poCurrent.Previous;
                }
                return m_poCurrent != null;
            }
        }

        public void Reset()
        {
            lock (m_poCP)
            {
                m_poCurrent = m_poLinkedList.First;
            }
        }

        public LinkedListNode<T> Node
        {
            get // 线程安全与增删节安全
            {
                lock (m_poCP)
                {
                    return m_poCurrent;
                }
            }
        }

        public T Value
        {
            get
            {
                lock (m_poCP)
                {
                    LinkedListNode<T> node = this.Node;
                    if (node == null)
                        return default(T);
                    return node.Value;
                }
            }
        }

        public bool Remove(LinkedListNode<T> node)
        {
            lock (m_poCP)
            {
                if (node == null)
                {
                    return false;
                }
                if (m_poCurrent == node)
                {
                    m_poCurrent = m_poCurrent.Next;
                }
                return m_poCurrent != null;
            }
        }

        public LinkedListIterator(object cp, LinkedList<T> linkedlist)
        {
            this.m_poCP = cp ?? throw new ArgumentNullException(nameof(cp));
            this.m_poLinkedList = linkedlist ?? throw new ArgumentNullException(nameof(linkedlist));
        }

        public static LinkedListIterator<T> operator ++(LinkedListIterator<T> iterator) // 移动指针到下一个节点
        {
            if (iterator != null)
            {
                lock (iterator.m_poCP)
                {
                    if (!iterator.MoveNext())
                    {
                        iterator.MoveNext();
                    }
                }
            }
            return iterator;
        }

        public static LinkedListIterator<T> operator --(LinkedListIterator<T> iterator) // 移动指针到上一个节点
        {
            if (iterator != null)
            {
                lock (iterator.m_poCP)
                {
                    if (!iterator.MovePrevious())
                    {
                        iterator.MovePrevious();
                    }
                }
            }
            return iterator;
        }
    }
}
