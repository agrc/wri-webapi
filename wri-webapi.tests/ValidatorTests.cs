using NUnit.Framework;
using wri_webapi.Configuration;
using wri_webapi.Models.Request;

namespace wri_webapi.tests
{
    [TestFixture]
    public class ValidatorTests
    {
        IAttributeValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new AttributeValidator();
        }

        [TestCase("POLY", "affected area", null, Result = true)]
        public bool ValidWithNoFeatureActions(string table, string type, FeatureActions[] actions)
        {
            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POLY", "terrestrial treatment area", null, Result = false)]
        [TestCase("POLY", "aquatic/riparian treatment area", null, Result = false)]
        [TestCase("POLY", "easement/acquisition", null, Result = false)]
        public bool InvalidWithoutActionAndTreatment(string table, string type, FeatureActions[] actions)
        {
            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POLY", "terrestrial treatment area", Result = true)]
        [TestCase("POLY", "aquatic/riparian treatment area", Result = true)]
        [TestCase("POLY", "easement/acquisition", Result = true)]
        public bool ValidWithActionAndTreatment(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions
                {
                    Action = "something",
                    Treatments = new[]
                    {
                        new FeatureTreatments
                        {
                            Treatment = "more"
                        }
                    }
                }
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POLY", "terrestrial treatment area", Result = true)]
        [TestCase("POLY", "aquatic/riparian treatment area", Result = true)]
        [TestCase("POLY", "easement/acquisition", Result = true)]
        public bool ValidWithMultipleActionAndTreatment(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions
                {
                    Action = "something",
                    Treatments = new[]
                    {
                        new FeatureTreatments
                        {
                            Treatment = "more"
                        }
                    }
                },
                new FeatureActions
                {
                    Action = "more",
                    Treatments = new[]
                    {
                        new FeatureTreatments
                        {
                            Treatment = "stuff",
                            Herbicides = new [] {"herb"}
                        }
                    }
                }
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POINT", "guzzler", Result = true)]
        [TestCase("POINT", "fish passage structure", Result = true)]
        [TestCase("LINE", "fence", Result = true)]
        [TestCase("LINE", "pipeline", Result = true)]
        [TestCase("LINE", "dam", Result = true)]
        public bool ValidWithActionAndType(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions
                {
                    Action = "construction",
                    Type = "barbed wire"
                }
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POINT", "guzzler", Result = false)]
        [TestCase("POINT", "trough", Result = false)]
        [TestCase("POINT", "fish passage structure", Result = false)]
        [TestCase("LINE", "fence", Result = false)]
        [TestCase("LINE", "pipeline", Result = false)]
        [TestCase("LINE", "dam", Result = false)]
        public bool InvalidWithoutActionAndType(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions()
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POINT", "water control struture", Result = true)]
        [TestCase("POINT", "other point feature", Result = true)]
        [TestCase("POINT", "trough", Result = true)]
        public bool ValidWithCommentsOnly(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions
                {
                    Description = "comments"
                }
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POINT", "water control struture", Result = false)]
        [TestCase("POINT", "other point feature", Result = false)]
        [TestCase("POINT", "trough", Result = false)]
        public bool InvalidWithoutComments(string table, string type)
        {
            var actions = new[]
            {
                new FeatureActions(), 
            };

            return _validator.ValidAttributesFor(table, type, actions);
        }

        [TestCase("POINT", "guzzler", Result = false)]
        [TestCase("POINT", "trough", Result = false)]
        [TestCase("POINT", "fish passage structure", Result = false)]
        [TestCase("POINT", "water control struture", Result = false)]
        [TestCase("POINT", "other point feature", Result = false)]
        [TestCase("LINE", "fence", Result = false)]
        [TestCase("LINE", "pipeline", Result = false)]
        [TestCase("LINE", "dam", Result = false)]
        public bool InvalidWithMultipleActions(string table, string type)
        {
            return _validator.ValidAttributesFor(table, type, new [] { new FeatureActions(), new FeatureActions() });
        }
    }
}