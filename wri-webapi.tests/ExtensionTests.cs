using NUnit.Framework;
using wri_webapi.Extensions;

namespace wri_webapi.tests
{
    [TestFixture]
    public class ExtensionTests
    {
        [Test]
        public void SquareMetersToAcres()
        {
            Assert.That("882965.823523".InAcres(), Is.EqualTo("218.1856 ac"));
        }

        [Test]
        public void SquareMetersToMiles()
        {
            Assert.That("882965.823523".InSquaredMiles(), Is.EqualTo("0.3409 mi²"));
        }

        [Test]
        public void MetersToFeet()
        {
            Assert.That("616.910978".InFeet(), Is.EqualTo("2,023.9821 ft"));
        }

        [Test]
        public void MetersToMiles()
        {
            Assert.That("616.910978".InMiles(), Is.EqualTo("0.3833 mi"));
        }
    }
}