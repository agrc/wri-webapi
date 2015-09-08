using System.Collections.ObjectModel;
using wri_soe.Models.FeatureClass;

namespace wri_soe.Commands
{
    /// <summary>
    ///     Adds the field index property to the feature class to index map
    /// </summary>
    public class UpdateLayerMapWithFieldIndexMapCommand
    {
        private readonly Collection<FeatureClassIndexMap> _map;

        public UpdateLayerMapWithFieldIndexMapCommand(Collection<FeatureClassIndexMap> map)
        {
            _map = map;
        }

        public void Execute()
        {
            foreach (var item in _map)
            {
                item.FieldMap = new FindIndexByFieldNameCommand(item.FeatureClass).Execute();
            }
        }
    }
}