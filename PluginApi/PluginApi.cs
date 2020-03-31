using Org.IdentityConnectors.Framework.Common.Objects;
using Org.IdentityConnectors.Framework.Spi;
using System.Collections.Generic;
namespace PluginApi
{
    public interface IConnectorPlugin
    {
        void OnBeforeCreate(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration);
        void OnBeforeUpdate(ObjectClass oclass, Uid uid, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration);
        void OnBeforeDelete(ObjectClass objClass, Uid uid, OperationOptions options, AbstractConfiguration connectorConfiguration);
        void OnBeforeAttributesReplace(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration);
        void OnBeforeAttributesAdd(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration);
        void OnBeforeAttributesDelete(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration);
    }
}
