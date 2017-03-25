using System;
using System.Collections.Generic;
using System.ComponentModel.Design;

namespace Microsoft.Web.XmlTransform
{
    internal class ServiceContainer : IServiceContainer, IDisposable
    {
        private Dictionary<Type, object> _serviceLookup = new Dictionary<Type, object>();

        public object GetService(Type serviceType)
        {
            object svc;
            if (_serviceLookup.TryGetValue(serviceType, out svc))
            {
                return svc;
            }

            return null;
        }

        public void AddService(Type serviceType, object serviceInstance)
        {
            AddService(serviceType, serviceInstance, false);
        }

        public void AddService(Type serviceType, object serviceInstance, bool promote)
        {
            if (!promote && _serviceLookup.ContainsKey(serviceType))
            {
                throw new ArgumentException("Service has already been added", nameof(serviceType));
            }

            _serviceLookup[serviceType] = serviceInstance;
        }

        public void AddService(Type serviceType, ServiceCreatorCallback callback)
        {
            AddService(serviceType, callback, false);
        }

        public void AddService(Type serviceType, ServiceCreatorCallback callback, bool promote)
        {
            if (!promote && _serviceLookup.ContainsKey(serviceType))
            {
                throw new ArgumentException("Service has already been added", nameof(serviceType));
            }

            _serviceLookup[serviceType] = callback;
        }

        public void RemoveService(Type serviceType)
        {
            RemoveService(serviceType, false);
        }

        public void RemoveService(Type serviceType, bool promote)
        {
            _serviceLookup.Remove(serviceType);
        }

        public void Dispose()
        {
            _serviceLookup.Clear();
        }
    }
}
