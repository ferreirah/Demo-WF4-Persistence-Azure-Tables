using System;

namespace MyLib.WF4.DurableInstancing
{
    public interface IStore
    {
        /// <summary>
        /// Enregister des données de persistance (Data,MetaData)
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="data"></param>
        /// <param name="metaData"></param>
        void SaveInstance(Guid instanceId, System.Xml.Linq.XDocument data, System.Xml.Linq.XDocument metaData);
        
        /// <summary>
        /// Chargement des données de persistance (Data,MetaData)
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="instanceData"></param>
        /// <param name="instanceMetaData"></param>
        /// <returns></returns>
        Boolean LoadInstance(Guid instanceId, out System.Xml.Linq.XDocument instanceData, out System.Xml.Linq.XDocument instanceMetaData);
        
        /// <summary>
        /// Enregister des données d'association entre id et key d'instance persistée 
        /// </summary>
        /// <param name="instanceId"></param>
        /// <param name="instanceKey"></param>
        void SaveInstanceAssociation(Guid instanceId, Guid instanceKey);
        
        /// <summary>
        /// Retourner l'id d'un instance persistée correspondant à la key d'instance
        /// </summary>
        /// <param name="instanceKey"></param>
        /// <returns></returns>
        Guid GetInstanceAssociation(Guid instanceKey);

        /// <summary>
        /// Supprimer toute information de persistance (Data,MetaData,Assocation)
        /// </summary>
        /// <param name="instanceId"></param>
        void DeleteInstance(Guid instanceId);
    }
}
