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

        [Test]
        public void NormalizesHerbicides()
        {
            var features = new[]
            {
                new SpatialFeature
                {
                    FeatureId = 4766,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Aerial (fixed-wing)",
                    Action = "Herbicide application",
                    Retreatment = true,
                    Herbicide = "2 4-D"
                }, new SpatialFeature
                {
                    FeatureId = 4766,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Ely (2-way)",
                    Action = "Anchor chainb",
                    Retreatment = true,
                    Herbicide = null
                }, new SpatialFeature
                {
                    FeatureId = 4767,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Aerial (fixed-wing)",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Aquaneat"
                }, new SpatialFeature
                {
                    FeatureId = 4767,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Aerial (fixed-wing)",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Milestone"
                }, new SpatialFeature
                {
                    FeatureId = 4767,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Aerial (helicopter)",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Aquaneat"
                }, new SpatialFeature
                {
                    FeatureId = 4768,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Aerial (fixed-wing)",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Grazon P+D"
                }, new SpatialFeature
                {
                    FeatureId = 4768,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Ground",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Outpost 22k"
                }, new SpatialFeature
                {
                    FeatureId = 4768,
                    Type = "Terrestrial Treatment Area",
                    SubType = "Spot treatment",
                    Action = "Herbicide application",
                    Retreatment = false,
                    Herbicide = "Pathfinder II"
                }, new SpatialFeature
                {
                    FeatureId = 4765,
                    Type = "Affected Area"
                }
            };

            var model = new ProjectWithFeaturesResponse
            {
                Features = features
            };

            var actual = model.Features.ToList();

            // there are 3 distinct terrestrials
            Assert.That(actual.Count(x=>x.Type == "Terrestrial Treatment Area"), Is.EqualTo(3));
            // one distinct affected area
            Assert.That(actual.Count(x=>x.Type == "Affected Area"), Is.EqualTo(1));
            // herbicide gets transfered to herbicides array
            Assert.That(actual.Single(x => x.FeatureId == 4766 && x.Action == "Herbicide application").Herbicides, Is.EquivalentTo(new[] { "2 4-D" }));
            // duplicate herbicides get simplified into one array
            Assert.That(actual.Single(x=>x.FeatureId == 4767 && x.SubType == "Aerial (fixed-wing)").Herbicides, Is.EquivalentTo(new []{ "Aquaneat", "Milestone"}));
            // Herbide should be empty and all items shoud lbe in herbicides property
            Assert.That(actual.Count(x=>!string.IsNullOrEmpty(x.Herbicide)), Is.EqualTo(0));
        }
    }
}
