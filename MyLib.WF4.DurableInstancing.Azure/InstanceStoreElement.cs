using System;
using System.ServiceModel.Activities;
using System.ServiceModel.Configuration;
using System.ServiceModel.Description;

namespace MyLib.WF4.DurableInstancing
{
    /// <summary>
    /// Element permetant d'ajouter l'InstanceStoreBehavior<T> dans un fichier de configuration
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InstanceStoreElement<T> : BehaviorExtensionElement where T : class, IStore, new()
    {
        /// <summary>
        /// Type du Behavior (InstanceStoreBehavior<T>)
        /// </summary>
        public override Type BehaviorType
        {
            get { return typeof(InstanceStoreBehavior<T>); }
        }

        /// <summary>
        /// Creation du Behavior de type InstanceStoreBehavior<T></T>
        /// </summary>
        /// <returns></returns>
        protected override object CreateBehavior()
        {
            return new InstanceStoreBehavior<T>();
        }
    }

    /// <summary>
    /// Behavior permettant d'ajouter un InstanceStore<T> au WorkflowServiceHost 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class InstanceStoreBehavior<T> : IServiceBehavior where T : class, IStore, new()
    {
        private readonly InstanceStore<T> m_InstanceStore;

        /// <summary>
        /// Constructeur
        /// </summary>
        public InstanceStoreBehavior()
        {
            // Instanciation de l'InstanceStore<T>
            m_InstanceStore = new InstanceStore<T>();
        }

        public void AddBindingParameters(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase, System.Collections.ObjectModel.Collection<ServiceEndpoint> endpoints, System.ServiceModel.Channels.BindingParameterCollection bindingParameters)
        {
            // Pas de paramètres
        }

        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
            // Ajout de l'instance de InstanceStore<T> au WorkflowServiceHost
            WorkflowServiceHost host = serviceHostBase as WorkflowServiceHost;
            if (host != null)
            {
                host.DurableInstancingOptions.InstanceStore = m_InstanceStore;
            }
        }

        public void Validate(ServiceDescription serviceDescription, System.ServiceModel.ServiceHostBase serviceHostBase)
        {
            // Pas de validation
        }
    }

}
