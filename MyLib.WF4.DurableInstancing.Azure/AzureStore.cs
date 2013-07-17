using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Table.DataServices;

namespace MyLib.WF4.DurableInstancing
{
    public class AzureStore : IStore
    {
        private const String c_TableAssociation = "WorkflowStoreAssociation";
        private const String c_TableInstance = "WorkflowStoreInstance";
        private const String c_PartitionKey = "WorkflowStore";

        private readonly TableServiceContext m_Context;

        /// <summary>
        ///     Constructeur
        /// </summary>
        public AzureStore()
        {
            try
            {
                // Récupération des information de compte pour Azure Table
                CloudStorageAccount account = CloudStorageAccount.DevelopmentStorageAccount;
                CloudTableClient cloudTableClient = account.CreateCloudTableClient();
                CloudTable table = cloudTableClient.GetTableReference(c_TableAssociation);
                table.CreateIfNotExists();
                CloudTable table1 = cloudTableClient.GetTableReference(c_TableInstance);
                table1.CreateIfNotExists();

                m_Context = cloudTableClient.GetTableServiceContext();

                // Cas de la DevFabric pour test avant publication sur Azure
                if (account.Credentials.AccountName == "devstoreaccount1")
                {
                    // Forcer l'ajout de données pour que le context soit utilisable en test
                    var associationRow = new AssociationRow
                        {
                            PartitionKey = c_PartitionKey,
                            RowKey = Guid.NewGuid().ToString()
                        };
                    var instanceRow = new InstanceRow
                        {
                            PartitionKey = c_PartitionKey,
                            RowKey = Guid.NewGuid().ToString()
                        };
                    m_Context.AddObject(c_TableAssociation, associationRow);
                    m_Context.AddObject(c_TableInstance, instanceRow);
                    m_Context.SaveChangesWithRetries();
                    m_Context.DeleteObject(associationRow);
                    m_Context.DeleteObject(instanceRow);
                    m_Context.SaveChangesWithRetries();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
            }
        }

        #region "Gestion des Azure Tables"

        /// <summary>
        ///     Azure Table des association
        /// </summary>
        private DataServiceQuery<AssociationRow> TableAssociation
        {
            get { return m_Context.CreateQuery<AssociationRow>(c_TableAssociation); }
        }

        /// <summary>
        ///     Azure Table des instances
        /// </summary>
        private DataServiceQuery<InstanceRow> TableInstance
        {
            get { return m_Context.CreateQuery<InstanceRow>(c_TableInstance); }
        }

        #endregion

        #region "Implémentation de IStore"

        /// <summary>
        ///     Enregister des données de persistance (Data,MetaData)
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="data"></param>
        /// <param name="metaData"></param>
        public void SaveInstance(Guid instanceId, XDocument data, XDocument metaData)
        {
            try
            {
                String rowKey = instanceId.ToString();

                // Recherche une ligne ne rapport avec l'instanceId
                IEnumerable<InstanceRow> rows = TableInstance
                    .Where(c => c.RowKey == rowKey)
                    .AsTableServiceQuery(m_Context)
                    .ToList();

                InstanceRow row = rows.FirstOrDefault();

                // test si une ligne correspond
                if (row == null)
                {
                    // Création d'une nouvelle ligne
                    row = new InstanceRow
                        {
                            PartitionKey = c_PartitionKey,
                            RowKey = rowKey,
                            Data = data,
                            MetaData = metaData
                        };

                    m_Context.AddObject(c_TableInstance, row);
                }
                else
                {
                    // Mise à jour d'une ligne existante
                    row.Data = data;
                    row.MetaData = metaData;
                    m_Context.UpdateObject(row);
                }

                // Enregistrer les changements
                m_Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Chargement des données de persistance (Data,MetaData)
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="instanceData"></param>
        /// <param name="instanceMetaData"></param>
        /// <returns></returns>
        public Boolean LoadInstance(Guid instanceId, out XDocument instanceData, out XDocument instanceMetaData)
        {
            try
            {
                String rowKey = instanceId.ToString();

                // Recherche une ligne ne rapport avec l'instanceId
                InstanceRow row = TableInstance
                    .Where(c => c.RowKey == rowKey)
                    .AsTableServiceQuery(m_Context)
                    .ToList()
                    .FirstOrDefault();

                // test si une ligne correspond
                if (row == null)
                {
                    instanceData = new XDocument();
                    instanceMetaData = new XDocument();
                    return false;
                }
                {
                    // Si oui on conserve les données
                    instanceData = row.Data;
                    instanceMetaData = row.MetaData;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Enregister des données d'association entre id et key d'instance persistée
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="instanceKey"></param>
        public void SaveInstanceAssociation(Guid instanceId, Guid instanceKey)
        {
            try
            {
                String rowKey = instanceId.ToString();

                // Création d'une nouvelle ligne
                var row = new AssociationRow
                    {
                        PartitionKey = c_PartitionKey,
                        RowKey = rowKey,
                        Key = instanceKey
                    };

                m_Context.AddObject(c_TableAssociation, row);

                // Enregistrer les changements
                m_Context.SaveChanges();
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Retourner l'id d'un instance persistée correspondant à la key d'instance
        /// </summary>
        /// <param name="instanceKey"></param>
        /// <returns></returns>
        public Guid GetInstanceAssociation(Guid instanceKey)
        {
            try
            {
                // Recherche une ligne ne rapport avec l'instanceId
                AssociationRow row = TableAssociation
                    .Where(c => c.Key == instanceKey)
                    .AsTableServiceQuery(m_Context)
                    .ToList()
                    .FirstOrDefault();

                // test si une ligne correspond
                if (row == null)
                {
                    return Guid.Empty;
                }
                {
                    return Guid.Parse(row.RowKey);
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     Supprimer toute information de persistance (Data,MetaData,Assocation)
        /// </summary>
        /// <param name="instanceId"></param>
        public void DeleteInstance(Guid instanceId)
        {
            String rowKey = instanceId.ToString();

            // Récupération de la liste des instance à supprimer
            List<InstanceRow> instanceRows = TableInstance
                .Where(c => c.RowKey == rowKey)
                .AsTableServiceQuery(m_Context)
                .ToList();

            foreach (InstanceRow row in instanceRows)
            {
                m_Context.DeleteObject(row);
            }

            // Récupération de la liste des association à supprimer
            List<AssociationRow> associationRows = TableAssociation
                .Where(c => c.RowKey == rowKey)
                .AsTableServiceQuery(m_Context)
                .ToList();

            foreach (AssociationRow row in associationRows)
            {
                m_Context.DeleteObject(row);
            }

            // Enregistrer les modifications
            m_Context.SaveChanges();
        }

        #endregion
    }
}