using System;
using System.Activities.DurableInstancing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.DurableInstancing;
using System.Xml.Linq;

namespace MyLib.WF4.DurableInstancing
{
    public class InstanceStore<T> : InstanceStore 
        where T : class, IStore, new()
    {
        
        // Id uniques utiles pour la méthode BindInstanceOwner()
        private readonly Guid m_InstanceOwnerId;
        private readonly Guid m_LockToken;

        // Store
        private readonly T m_Store;

        /// <summary>
        /// Constructeur
        /// </summary>
        public InstanceStore()
        {
            m_Store = new T();
            m_LockToken = Guid.NewGuid();
            m_InstanceOwnerId = Guid.NewGuid();
        }

        #region "Gestion des commandes demandée par l'hote d'instances de workflows"

        /// <summary>
        /// Demande de traitement d'une commande
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        protected override Boolean TryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout)
        {
            try
            {
                // Demande de création d'un propriétaire d'une instance de workflow
                if (command is CreateWorkflowOwnerCommand)
                {
                    CreateWorkflowOwner(context, command as CreateWorkflowOwnerCommand);
                }
                // Demande de chargement d'une instance de workflow pesistée
                else if (command is LoadWorkflowCommand)
                {
                    LoadWorkflow(context, command as LoadWorkflowCommand);
                }
                // Demande de chargement d'une instance de workflow pesistée via une key connue
                else if (command is LoadWorkflowByInstanceKeyCommand)
                {
                    LoadWorkflowByInstanceKey(context, command as LoadWorkflowByInstanceKeyCommand);
                }
                // Demande de persistence d'une instance de workflow
                else if (command is SaveWorkflowCommand)
                {
                    SaveWorkflow(context, command as SaveWorkflowCommand);
                }
                // autres demandes
                else
                {
                    return base.TryCommand(context, command, timeout);
                }
                return true;
            }
            catch (InstancePersistenceException ex)
            {
                Trace.TraceError(ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Demande de traitement d'une commande (Async)
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        /// <param name="timeout"></param>
        /// <param name="callback"></param>
        /// <param name="state"></param>
        /// <returns></returns>
        protected override IAsyncResult BeginTryCommand(InstancePersistenceContext context, InstancePersistenceCommand command, TimeSpan timeout, AsyncCallback callback, object state)
        {
            Func<InstancePersistenceContext, InstancePersistenceCommand, TimeSpan, Boolean> f = TryCommand;

            return f.BeginInvoke(
                context,
                command,
                timeout,
                r => { if(callback!=null) callback(new InstanceStoreAsyncResult(r, f.EndInvoke(r))); },
                state);
        }

        /// <summary>
        /// Fin de demande de traitement d'une commande (Async)
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected override Boolean EndTryCommand(IAsyncResult result)
        {
            var asyncResult = result as InstanceStoreAsyncResult;
            if (asyncResult != null)
            {
                return asyncResult.Value;
            }
            return true;
        }

        #endregion

        #region "Execution des commandes demandées par les hotes de workflow"
        
        /// <summary>
        /// Création d'un propriétaire d'une instance de workflow
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        private void CreateWorkflowOwner(InstancePersistenceContext context, CreateWorkflowOwnerCommand command)
        {
            context.BindInstanceOwner(m_InstanceOwnerId, m_LockToken);
        }

        /// <summary>
        /// Chargement d'une instance de workflow persistée
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        private void LoadWorkflow(InstancePersistenceContext context, LoadWorkflowCommand command)
        {
            if (command.AcceptUninitializedInstance)
            {
                context.LoadedInstance(
                    InstanceState.Uninitialized,
                    null,
                    null,
                    null,
                    null);
            }
            else
            {
                LoadWorkflow(context, context.InstanceView.InstanceId);
            }
        }

        /// <summary>
        /// Chargement d'une instance de workflow persistée identifiée via sa key
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        private void LoadWorkflowByInstanceKey(InstancePersistenceContext context, LoadWorkflowByInstanceKeyCommand command)
        {
            // Recherche de id d'instance de workflow
            Guid instanceId = m_Store.GetInstanceAssociation(
                command.LookupInstanceKey);

            if (instanceId == Guid.Empty)
            {
                throw new InstanceKeyNotReadyException(
                    String.Format(
                        "Impossible to change the instance with key : {0}",
                        command.LookupInstanceKey));
            }

            // Chargement d'une instance de workflow persisstée
            LoadWorkflow(context, instanceId);
        }

        /// <summary>
        /// Persistance d'une instance de workflow
        /// </summary>
        /// <param name="context"></param>
        /// <param name="command"></param>
        private void SaveWorkflow(InstancePersistenceContext context, SaveWorkflowCommand command)
        {
            // Récupération de l'id d'instance
            Guid instanceId = context.InstanceView.InstanceId;

            // Test si l'instance s'est terminée normalement
            if (command.CompleteInstance)
            {
                m_Store.DeleteInstance(instanceId);
                return;
            }

            // Test si on a des Data et MetaData à persister
            if (command.InstanceData.Count > 0
                || command.InstanceMetadataChanges.Count > 0)
            {
                // Persitance des Data et MetaData
                XDocument docData;
                XDocument docMetaData;
                InstanceStoreSerializer.GetDataToSave(
                    command.InstanceData,
                    command.InstanceMetadataChanges,
                    out docData,
                    out docMetaData);

                m_Store.SaveInstance(instanceId, docData, docMetaData);
            }

            // Test si on a bien de keys a associer à l'instance de workflow
            if (command.InstanceKeysToAssociate.Count > 0)
            {
                // Persistance de chaque key devant être associée à l'instance de workflow persistée
                foreach (var entry in command.InstanceKeysToAssociate)
                {
                    m_Store.SaveInstanceAssociation(
                        instanceId,
                        entry.Key);
                }
            }
        }

        /// <summary>
        /// Chargement d'une instance de workflow persistée
        /// </summary>
        /// <param name="context"></param>
        /// <param name="instanceId"></param>
        private void LoadWorkflow(InstancePersistenceContext context, Guid instanceId)
        {
            // Test si on a bien une instance déterminée
            if (instanceId != Guid.Empty)
            {
                IDictionary<XName, InstanceValue> instanceData;
                IDictionary<XName, InstanceValue> instanceMetaData;

                // Chargement de la Data et de la MetaData 
                XDocument xmlData;
                XDocument xmlMetaData;

                // Chargement des Data et MetaData de l'instance persitée
                if (m_Store.LoadInstance(instanceId, out xmlData, out xmlMetaData))
                {
                    InstanceStoreSerializer.GetDataToLoad(
                        xmlData, 
                        xmlMetaData, 
                        out instanceData,
                        out instanceMetaData);
                }
                else
                {
                    instanceData = new Dictionary<XName, InstanceValue>();
                    instanceMetaData = new Dictionary<XName, InstanceValue>();
                }

                // Binding de l'instance à charger avec le context de persistance
                if (context.InstanceView.InstanceId == Guid.Empty)
                {
                    context.BindInstance(instanceId);
                }

                // Chargement du workflow
                context.LoadedInstance(
                    InstanceState.Initialized,
                    instanceData,
                    instanceMetaData,
                    null,
                    null);
            }
            else
            {
                throw new InstanceNotReadyException("Impossible to change the instance without Id!");
            }
        }

        #endregion
    }
}
