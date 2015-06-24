using System.Linq;
using NUnit.Framework;
using wri_webapi.Models.Database;
using wri_webapi.Models.Response;

namespace wri_webapi.tests
{
    [TestFixture]
    public class ProjectWithFeaturesModelTests
    {
        [Test]
        public void AddsParentIdToOneToManyPolygonAttributes()
        {
            var features = new[]
            {
                new SpatialFeature
                {
                    FeatureId = 1,
                    Type = "Terrestrial",
                    SubType = "one",
                    Action = "a"
                },
                new SpatialFeature
                {
                    FeatureId = 1,
                    Type = "Terrestrial",
                    SubType = "one",
                    Action = "b"
                },
                new SpatialFeature
                {
                    FeatureId = 1,
                    Type = "Terrestrial",
                    SubType = "one",
                    Action = "c"
                },
                new SpatialFeature
                {
                    FeatureId = 2,
                    Type = "Terrestrial",
                    SubType = "two",
                    Action = "a"
                }
            };

            var model = new ProjectWithFeaturesResponse
            {
                Features = features
            };

            var actual = model.Features.ToList();

            // there should be one more item since a header is added for items with related data
            Assert.That(actual.Count(), Is.EqualTo(5));

            var header = actual.Single(x => x.Id == 4);
            Assert.That(header.FeatureId, Is.EqualTo(1));
            Assert.That(header.HasChildren, Is.True);
            Assert.That(header.Parent, Is.Null);
            Assert.That(header.Type, Is.EqualTo("Terrestrial"));

            // id's are added to each feature starting from 0. the header gets the max+1 value
            Assert.That(actual.Count(x => x.Parent == 4), Is.EqualTo(3));
        }
    }
}
