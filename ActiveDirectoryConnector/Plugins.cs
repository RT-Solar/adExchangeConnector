using Org.IdentityConnectors.Framework.Common.Objects;
using Org.IdentityConnectors.Framework.Spi;
using PluginApi;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Org.IdentityConnectors.ActiveDirectory
{
    public class Plugins : IConnectorPlugin
    {
        private static readonly NLog.Logger LOG = NLog.LogManager.GetCurrentClassLogger();

        private ICollection<IConnectorPlugin> loadedPlugins = new List<IConnectorPlugin>();

        public void LoadPlugins(string directory)
        {
            LOG.Info("Loading plugins from directory: " + directory);
            foreach (string csFile in GetAllCsFiles(directory))
            {
                IConnectorPlugin plugin = TryLoadPlugin(csFile);
                if (plugin != null)
                {
                    loadedPlugins.Add(plugin);
                    LOG.Info("Successfully loaded plugin: " + plugin.GetType().Name);
                }
                else
                {
                    LOG.Info("Plugin has not been loaded due to previous errors: " + directory);
                }
            }
            LOG.Info("Done loading plugins from directory: " + directory);
        }

        public void OnBeforeCreate(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach(var plugin in loadedPlugins) plugin.OnBeforeCreate(oclass, attributes, options, connectorConfiguration);
        }

        public void OnBeforeUpdate(ObjectClass oclass, Uid uid, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach(var plugin in loadedPlugins) plugin.OnBeforeUpdate(oclass, uid, attributes, options, connectorConfiguration);
        }

        public void OnBeforeDelete(ObjectClass objClass, Uid uid, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach (var plugin in loadedPlugins) plugin.OnBeforeDelete(objClass, uid, options, connectorConfiguration);
        }

        public void OnBeforeAttributesReplace(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach (var plugin in loadedPlugins) plugin.OnBeforeAttributesReplace(oclass, attributes, options, connectorConfiguration);
        }

        public void OnBeforeAttributesAdd(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach (var plugin in loadedPlugins) plugin.OnBeforeAttributesAdd(oclass, attributes, options, connectorConfiguration);
        }

        public void OnBeforeAttributesDelete(ObjectClass oclass, ICollection<ConnectorAttribute> attributes, OperationOptions options, AbstractConfiguration connectorConfiguration)
        {
            foreach (var plugin in loadedPlugins) plugin.OnBeforeAttributesDelete(oclass, attributes, options, connectorConfiguration);
        }

        private IConnectorPlugin TryLoadPlugin(string file)
        {
            LOG.Info("Loading plugin from file: " + file);
            CompilerResults compilerResults = CompileFile(file);
            try
            {
                if (compilerResults.CompiledAssembly == null)
                {
                    throw new Exception("Compiled assembly is null");
                }
                if (compilerResults.CompiledAssembly.GetTypes() == null
                    || compilerResults.CompiledAssembly.GetTypes().Count() == 0)
                {
                    throw new Exception("Compiled assembly contains no types");
                }
                Type compiledType = compilerResults.CompiledAssembly.GetTypes().First();
                object pluginInstance = Activator.CreateInstance(compiledType);
                if (!(pluginInstance is IConnectorPlugin))
                {
                    throw new Exception("Plugin instance is expected to be IConnectorPlugin, but it is " + compiledType.GetType());
                }
                return (IConnectorPlugin) pluginInstance;
            }
            catch (Exception e)
            {
                LOG.Error(e, "Could not compile plugin: " + file);
                foreach(var error in compilerResults.Errors)
                {
                    LOG.Error("Compilation error: " + error);
                }
                return null;
            }
        }

        private string[] GetAllCsFiles(string directory)
        {
            return Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);
        }

        private CompilerResults CompileFile(string filepath)
        {
            string language = CodeDomProvider.GetLanguageFromExtension(Path.GetExtension(filepath));
            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider(language);
            CompilerParameters compilerParams = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = true,
                IncludeDebugInformation = false
            };

            compilerParams.ReferencedAssemblies.AddRange(
                Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(a => a.Name + ".dll").ToArray()
            );
            compilerParams.ReferencedAssemblies.Add("ActiveDirectory.Connector.dll");

            return codeDomProvider.CompileAssemblyFromFile(compilerParams, filepath);
        }
    }
}
