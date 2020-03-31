using Org.IdentityConnectors.ActiveDirectory;
using Org.IdentityConnectors.Framework.Common.Objects;
using Org.IdentityConnectors.Framework.Spi;
using PluginApi;
using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Linq;

namespace RnAdConnectorPlugin
{
    class RnAdConnectorPlugin : IConnectorPlugin
    {
        private static readonly NLog.Logger LOG = NLog.LogManager.GetCurrentClassLogger();

        public RnAdConnectorPlugin()
        {
            LOG.Info("RnAdConnectorPlugin initialized");
        }

        public void OnBeforeAttributesAdd(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
        }

        public void OnBeforeAttributesDelete(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
        }

        public void OnBeforeAttributesReplace(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
        }

        public void OnBeforeCreate(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            LOG.Info("OnBeforeCreate started");
            AddOrReplaceDatabase(oclass, null, attributes, options, connectorConfiguration);
            LOG.Info("OnBeforeCreate finished");
        }

        public void OnBeforeUpdate(ObjectClass oclass, Uid uid, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            LOG.Info("OnBeforeUpdate started");
            AddOrReplaceDatabase(oclass, uid, attributes, options, connectorConfiguration);
            LOG.Info("OnBeforeUpdate finished");
        }

        public void OnBeforeDelete(ObjectClass objClass, Uid uid, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
        }

        private ConnectorAttribute GetAttribute(ICollection<ConnectorAttribute> attributes, string attributeName)
        {
            try
            {
                return attributes.First(attr => attr.Name.Equals(attributeName));
            }
            catch (InvalidOperationException)
            {
                return null;
            } 
        }

        private void AddOrReplaceAttribute(ICollection<ConnectorAttribute> attributes, string name, string value)
        {
            ConnectorAttribute attribute = GetAttribute(attributes, name);
            if(attribute == null)
            {
                attributes.Add(ConnectorAttributeBuilder.Build(name, new string[] { value }));
            }
            else
            {
                attribute.Value.Clear();
                attribute.Value.Add(value);
            }
        }

        private string GetDefaultHomeMdb(DirectoryEntry entry)
        {
            try
            {
                const string adProperty = "snpa-VarSet";
                const string targetPropertyStartsWith = "defaulthomemdb=";
                foreach (string property in entry.Properties[adProperty])
                {
                    string propertyNameLower = property.ToLower();
                    LOG.Info("Property name lower is: " + propertyNameLower);
                    LOG.Info("Starts with " + targetPropertyStartsWith + ": " + propertyNameLower.StartsWith(targetPropertyStartsWith));
                    if (propertyNameLower.StartsWith(targetPropertyStartsWith))
                    {
                        return property.Substring(targetPropertyStartsWith.Length);
                    }
                }
                return null;
            } catch(Exception e)
            {
                LOG.Info("Error: " + e.Message);
                LOG.Info("Returning null");
                return null;
            }
        }

        private void AddOrReplaceDatabase(ObjectClass oclass, Uid uid, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            if (!ObjectClass.ACCOUNT.Equals(oclass))
            {
                LOG.Info("Nothing to do");
                return;
            }
            // вычисляем значение атрибута Database только если он не был передан
            // и дополнительно пришёл атрибут RecipientType со значением UserMailbox
            ConnectorAttribute database = GetAttribute(attributes, "Database");
            ConnectorAttribute recipientType = GetAttribute(attributes, "RecipientType");
            if (database != null || recipientType == null || !recipientType.Value.First().ToString().Equals("UserMailbox"))
            {
                LOG.Info("Nothing to do");
                return;
            }

            DirectoryEntry container = null;
            try
            {
                ActiveDirectoryConfiguration config = (ActiveDirectoryConfiguration)connectorConfiguration;

                // вычисляем dn, для update'а находим по uid
                string dn = "";
                if (uid != null)
                {
                    DirectoryEntry entry = new DirectoryEntry(ActiveDirectoryUtils.GetLDAPPath(config.LDAPHostName, uid.GetUidValue()), config.DirectoryAdminName, config.DirectoryAdminPassword);
                    dn = (string)entry.Properties["distinguishedName"][0];
                    entry.Dispose();
                }
                else
                {
                    dn = GetAttribute(attributes, "__NAME__").Value.First().ToString();
                }
                string parentDn = ActiveDirectoryUtils.GetParentDn(dn);
                string ldapContainerPath = ActiveDirectoryUtils.GetLDAPPath(config.LDAPHostName, parentDn);

                container = new DirectoryEntry(ldapContainerPath, config.DirectoryAdminName, config.DirectoryAdminPassword);

                // поиск значения Database в родительских OU
                string defaultHomeMdb = null;
                while (defaultHomeMdb == null && container != null)
                {
                    LOG.Info("Looking for DefaultHomeMdb in {0}", container.Path);
                    defaultHomeMdb = GetDefaultHomeMdb(container);
                    if (defaultHomeMdb != null)
                    {
                        LOG.Info("Found! DefaultHomeMdb = {0} (in container {1})", defaultHomeMdb, container.Path);
                    }
                    else
                    {
                        LOG.Info("Did not found DefaultHomeMdb in container {0}", container.Path);
                    }
                    try
                    {
                        container = container.Parent;
                    }
                    catch (Exception e)
                    {
                        LOG.Info("Error: " + e.Message);
                        container = null;
                    }
                }

                // установка значения атрибута, если не нашли указываем значение по умолчанию
                if (defaultHomeMdb != null)
                {
                    LOG.Info("Setting DefaultHomeMdb: " + defaultHomeMdb);
                    AddOrReplaceAttribute(attributes, "Database", defaultHomeMdb);
                }
                else
                {
                    LOG.Info("Did not found DefaultHomeMdb, will set default value");
                    AddOrReplaceAttribute(attributes, "Database", "MSK-RN-DAG01-1GB-02");
                }
            }
            finally
            {
                if (container != null)
                {
                    container.Dispose();
                }
            }
        }
    }
}
