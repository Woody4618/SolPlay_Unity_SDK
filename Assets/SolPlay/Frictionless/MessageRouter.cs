using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Frictionless
{
	public class MessageRouter : MonoBehaviour
	{
		private Dictionary<Type,List<MessageHandler>> handlers = new Dictionary<Type, List<MessageHandler>>();
		private List<Delegate> pendingRemovals = new List<Delegate>();
		private bool isRaisingMessage;

		public void Awake()
		{
			ServiceFactory.Instance.RegisterSingleton(this);
		}

		public void AddHandler<T>(Action<T> handler)
		{
			List<MessageHandler> delegates = null;
			if (!handlers.TryGetValue(typeof(T), out delegates))
			{
				delegates = new List<MessageHandler>();
				handlers[typeof(T)] = delegates;
			}
			if (delegates.Find(x => x.Delegate == handler) == null)
				delegates.Add(new MessageHandler() { Target = handler.Target, Delegate = handler });
		}

		public void RemoveHandler<T>(Action<T> handler)
		{
			List<MessageHandler> delegates = null;
			if (handlers.TryGetValue(typeof(T), out delegates))
			{
				MessageHandler existingHandler = delegates.Find(x => x.Delegate == (Delegate) handler);
				if (existingHandler != null)
				{
					if (isRaisingMessage)
						pendingRemovals.Add(handler);
					else
						delegates.Remove(existingHandler);
				}
			}
		}

		public void Reset()
		{
			handlers.Clear();
		}

		public void RaiseMessage(object msg)
		{
			try
			{
				List<MessageHandler> delegates = null;
				if (handlers.TryGetValue(msg.GetType(), out delegates))
				{
					isRaisingMessage = true;
					try
					{
						for (int i = delegates.Count -1; i >= 0; i--)
						{
							MessageHandler messageHandler = delegates[i];
#if NETFX_CORE
							h.Delegate.DynamicInvoke(msg);
#else
							messageHandler.Delegate.Method.Invoke(messageHandler.Target, new object[] { msg });
#endif	
						}
						/*foreach (MessageHandler messageHandler in delegates)
						{
	#if NETFX_CORE
							h.Delegate.DynamicInvoke(msg);
	#else
							messageHandler.Delegate.Method.Invoke(messageHandler.Target, new object[] { msg });
	#endif
						}*/
					}
					finally
					{
						isRaisingMessage = false;
					}

					foreach (Delegate d in pendingRemovals)
					{
						foreach (KeyValuePair<Type, List<MessageHandler>> entry in handlers)
						{
							for (int i = entry.Value.Count -1; i >= 0; i--)
							{
								MessageHandler array = entry.Value[i];
								if (array.Delegate == d)
								{
									entry.Value.RemoveAt(i);
								}
							}
							
							/*MessageHandler existingHandler = entry.Value.Find(x => x.Delegate == (Delegate) d);
							if (existingHandler != null)
							{
								entry.Value.Remove(existingHandler);
							}*/
						}
					}
					pendingRemovals.Clear();
				}
			}
			catch(Exception ex)
			{
				UnityEngine.Debug.LogError("Exception while raising message " + msg + ": " + ex);
			}
		}

		public class MessageHandler
		{
			public object Target { get; set; }
			public Delegate Delegate { get; set; }
		}
	}
}
