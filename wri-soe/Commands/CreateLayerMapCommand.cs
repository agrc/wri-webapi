using System;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.SOESupport;
using wri_soe.Models.FeatureClass;

namespace wri_soe.Commands
{
    /// <summary>
    ///     Maps the layers in the host soe mxd to an index
    /// </summary>
    public class CreateLayerMapCommand
    {
        private readonly IServerObjectHelper _serverObjectHelper;

        private IMapServerDataAccess _dataAccess;
        private const int MessageCode = 1337;
        private string _defaultMapName;
        private readonly ServerLogger _logger;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CreateLayerMapCommand" /> class.
        /// </summary>
        /// <param name="serverObjectHelper">The server object helper.</param>
        public CreateLayerMapCommand(IServerObjectHelper serverObjectHelper)
        {
            _serverObjectHelper = serverObjectHelper;
            _logger = new ServerLogger();
        }

        /// <summary>
        ///     code to execute when command is run.
        /// </summary>
        /// <exception cref="System.NullReferenceException">Map service was not found.</exception>
        public Collection<FeatureClassIndexMap> Execute()
        {
            var mapServer = _serverObjectHelper.ServerObject as IMapServer3;

            if (mapServer == null)
            {
#if !DEBUG
                _logger.LogMessage(ServerLogger.msgType.error, "CreateLayerMapCommand.Execute", MessageCode, "Map service not found.");
#endif
                throw new NullReferenceException("Map service was not found.");
            }

            _dataAccess = (IMapServerDataAccess)mapServer;

            if (_dataAccess == null)
            {
#if !DEBUG
                _logger.LogMessage(ServerLogger.msgType.error, "CreateLayerMapCommand.Execute", MessageCode, "Problem accessing IMapServerDataAccess object.");
#endif
                throw new NullReferenceException("Problem accessing IMapServerDataAccess object.");
            }

            var result = new Collection<FeatureClassIndexMap>();

            _defaultMapName = mapServer.DefaultMapName;
#if !DEBUG
            _logger.LogMessage(ServerLogger.msgType.infoStandard, "CreateLayerMapCommand.Execute", MessageCode, string.Format("default map name: {0}", _defaultMapName));
#endif
            var layerInfos = mapServer.GetServerInfo(_defaultMapName).MapLayerInfos;

            var count = layerInfos.Count;

            for (var i = 0; i < count; i++)
            {
                var layerInfo = layerInfos.Element[i];
#if !DEBUG
                _logger.LogMessage(ServerLogger.msgType.infoStandard, "CreateLayerMapCommand.Execute", MessageCode, string.Format("layerInfo name: {0}", layerInfo.Name));
#endif
                if (layerInfo.IsComposite)
                {
                    continue;
                }

                result.Add(new FeatureClassIndexMap(i, layerInfo.Name, GetFeatureClassFromMap(i)));
            }

            new UpdateLayerMapWithFieldIndexMapCommand(result).Execute();

            return result;
        }

        private IFeatureClass GetFeatureClassFromMap(int layerIndex)
        {
            var featureClass = _dataAccess.GetDataSource(_defaultMapName, layerIndex) as IFeatureClass;

            if (featureClass == null)
            {
#if !DEBUG
               _logger.LogMessage(ServerLogger.msgType.error, "CreateLayerMapCommand.GetFeatureClassFromMap", MessageCode, string.Format("featureclass cannot be null: {0}", layerIndex));
#endif
                throw new NullReferenceException("FeatureClass cannot be null");
            }

            return featureClass;
        }
    }
}