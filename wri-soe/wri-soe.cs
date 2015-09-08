﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Server;
using ESRI.ArcGIS.SOESupport;
using Newtonsoft.Json;
using wri_shared.Models.Response;
using wri_soe.Commands;
using wri_soe.Encoding;
using wri_soe.Equality;
using wri_soe.Models.FeatureClass;

namespace wri_soe
{
    [ComVisible(true)]
    [Guid("9cc780be-61a7-444e-9646-9e9691512be5")]
    [ClassInterface(ClassInterfaceType.None)]
    [ServerObjectExtension("MapServer",
        AllCapabilities = "",
        DefaultCapabilities = "",
        Description = "Do advanced things that can only be done with arcobjects",
        DisplayName = "wri.soe",
        Properties = "",
        SupportsREST = true,
        SupportsSOAP = false)]
    public class wri_soe : JsonEndpoint, IServerObjectExtension, IObjectConstruct, IRESTRequestHandler
    {
        private const string Version = "0.1.0";
        private const int MessageCode = 1337;
        private readonly IRESTRequestHandler _reqHandler;
        private readonly string _soeName;
        private IPropertySet _configProps;
        private Collection<FeatureClassIndexMap> _featureClassIndexMap;
        private readonly ServerLogger _logger;
        private IServerObjectHelper _serverObjectHelper;

        public wri_soe()
        {
            _soeName = GetType().Name;
            _logger = new ServerLogger();
            _reqHandler = new SoeRestImpl(_soeName, CreateRestSchema());
        }

        public void Construct(IPropertySet props)
        {
            _configProps = props;
            _featureClassIndexMap = new CreateLayerMapCommand(_serverObjectHelper).Execute();
        }

        public string GetSchema()
        {
            return _reqHandler.GetSchema();
        }

        public byte[] HandleRESTRequest(string capabilities, string resourceName, string operationName,
            string operationInput, string outputFormat, string requestProperties, out string responseProperties)
        {
            return _reqHandler.HandleRESTRequest(capabilities, resourceName, operationName, operationInput, outputFormat,
                requestProperties, out responseProperties);
        }

        public void Init(IServerObjectHelper pSOH)
        {
            _serverObjectHelper = pSOH;
        }

        public void Shutdown()
        {
        }

        private RestResource CreateRestSchema()
        {
            var resource = new RestResource(_soeName, false, RootHandler);

            var operation = new RestOperation("ExtractIntersections",
                new[] {"geometry", "criteria"},
                new[] {"json"},
                Extracthandler);

            resource.operations.Add(operation);

            return resource;
        }

        private byte[] RootHandler(NameValueCollection boundVariables, string outputFormat, string requestProperties,
            out string responseProperties)
        {
            responseProperties = null;

            return System.Text.Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                Description = "Extract intersection information",
                CreatedBy = "AGRC - Steve Gourley @steveAGRC",
                Version
            }));
        }

        private byte[] Extracthandler(NameValueCollection boundVariables,
            JsonObject operationInput,
            string outputFormat,
            string requestProperties,
            out string responseProperties)
        {
            responseProperties = null;
            var errors = new ResponseContainer(HttpStatusCode.BadRequest, "");

            string base64Geometry;
            var found = operationInput.TryGetString("geometry", out base64Geometry);

            if (!found || string.IsNullOrEmpty(base64Geometry))
            {
                errors.Message = "geometry parameter is required.";

                return Json(errors);
            }

            JsonObject queryCriteria;
            found = operationInput.TryGetJsonObject("criteria", out queryCriteria);

            if (!found)
            {
                errors.Message = "criteria parameter is required.";

                return Json(errors);
            }

#if !DEBUG
            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, "Params received");
#endif

            IGeometry geometry;
            int read;
            var factory = new GeometryEnvironmentClass() as IGeometryFactory3;
            factory.CreateGeometryFromWkbVariant(Convert.FromBase64String(base64Geometry), out geometry, out read);

#if !DEBUG
            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, "Geometry converted");
#endif

            var filter = new SpatialFilter
            {
                Geometry = geometry,
                SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects
            };

            var notEsri = JsonConvert.DeserializeObject<Dictionary<string, string[]>>(queryCriteria.ToJson());
            var searchResults = new Dictionary<string, IList<IntersectAttributes>>();

            foreach (var keyValue in notEsri)
            {
                var container = _featureClassIndexMap.Single(x => x.Index == int.Parse(keyValue.Key));
                var fields = keyValue.Value.Select(x => x.ToUpper());
                var fieldMap = container.FieldMap.Select(x => x.Value)
                    .Where(y => fields.Contains(y.Field.ToUpper()))
                    .ToList();

#if !DEBUG
                _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, string.Format("Querying {0} at index {1}", container.LayerName, container.Index));
#endif

                var cursor = container.FeatureClass.Search(filter, true);
                IFeature feature;
                while ((feature = cursor.NextFeature()) != null)
                {
                    var values = new GetValueAtIndexCommand(fieldMap, feature).Execute();
                    var attributes = new IntersectAttributes(values);

                    // line over polygon = 1D
                    // polygon over polygon = 2D

                    switch (geometry.GeometryType)
                    {
                        case esriGeometryType.esriGeometryPolygon:
                        {
#if !DEBUG
                            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, "User input polygon, intersecting " + container.LayerName);
#endif
                            var gis = (ITopologicalOperator4)geometry;
                            gis.Simplify();

                            if (feature.ShapeCopy.GeometryType == esriGeometryType.esriGeometryPolygon)
                            {
                                try
                                {
                                    var intersection =
                                        (IArea) gis.Intersect(feature.ShapeCopy, esriGeometryDimension.esriGeometry2Dimension);

                                    attributes.Intersect = Math.Abs(intersection.Area);
#if !DEBUG
                                    _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, string.Format("Area: {0}", intersection.Area));
#endif
                                }
                                catch (Exception ex)
                                {
                                    return Json(new ResponseContainer(HttpStatusCode.InternalServerError, ex.Message));
                                }
                            }
                            else if (feature.ShapeCopy.GeometryType == esriGeometryType.esriGeometryPolyline)
                            {

                                var intersection = (IPolyline5)gis.Intersect(feature.ShapeCopy, esriGeometryDimension.esriGeometry1Dimension);

#if !DEBUG
                                _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, string.Format("Length: {0}", intersection.Length));
#endif

                                attributes.Intersect = Math.Abs(intersection.Length);
                            }

                        }
                            break;
                        case esriGeometryType.esriGeometryPolyline:
                        {
#if !DEBUG
                            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, "User input polyline, acting on " + container.LayerName);
#endif

                            var gis = (ITopologicalOperator5) geometry;
                            gis.Simplify();

                            var intersection = (IPolyline) gis.Intersect(feature.ShapeCopy, esriGeometryDimension.esriGeometry1Dimension);

#if !DEBUG
                            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, string.Format("Length: {0}", intersection.Length));
#endif

                            attributes.Intersect = Math.Abs(intersection.Length);
                        }
                            break;
                    }

                    if (searchResults.ContainsKey(container.LayerName))
                    {
                        if (searchResults[container.LayerName].Any(x => new MultiSetComparer<object>().Equals(x.Attributes, attributes.Attributes)))
                        {
                            var duplicate = searchResults[container.LayerName]
                                .Single(x => new MultiSetComparer<object>().Equals(x.Attributes, attributes.Attributes));

                            duplicate.Intersect += attributes.Intersect;
                        }
                        else
                        {
                            searchResults[container.LayerName].Add(attributes);
                        }
                    }
                    else
                    {
                        searchResults[container.LayerName] = new Collection<IntersectAttributes> {attributes};
                    }
                }
            }

            var response = new IntersectResponse(searchResults);

#if !DEBUG
            _logger.LogMessage(ServerLogger.msgType.infoStandard, "Extracthandler", MessageCode, string.Format("Returning results {0}", searchResults.Count));
#endif

            return Json(new ResponseContainer<IntersectResponse>(response));
        }
    }
}