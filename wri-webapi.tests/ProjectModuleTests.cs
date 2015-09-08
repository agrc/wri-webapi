using Nancy;
using Nancy.Testing;
using NUnit.Framework;
using wri_webapi.Models.Response;

namespace wri_webapi.tests
{
    [TestFixture]
    public class ProjectModuleTests
    {
        [TestFixture]
        public class SlashProject
        {
            private Browser _browser;

            [SetUp]
            public void Setup()
            {
                var bootstrapper = new Bootstrapper();
                _browser = new Browser(bootstrapper, to => to.Accept("application/json"));
            }

            [Test]
            public void ReturnsTheCorrectProject()
            {
                var result = _browser.Get("/project/3207", with =>
                {
                    with.HttpRequest();
                });

                var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(model.Project.ProjectId, Is.EqualTo(3207));
            }

            [TestFixture]
            public class AllowEdits
            {
                private Browser _browser;

                [SetUp]
                public void Setup()
                {
                    var bootstrapper = new Bootstrapper();
                    _browser = new Browser(bootstrapper, to => to.Accept("application/json"));
                }

                [Test]
                public void IsFalseIfNoUserDataIsSent()
                {
                    var result = _browser.Get("/project/3207", with =>
                    {
                        with.HttpRequest();
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfPartialUserDataIsSent()
                {
                    var result = _browser.Get("/project/3207", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "partial");
                        with.Query("token", null);
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsTrueForPmOwner()
                {
                    var result = _browser.Get("/project/1", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "pm");
                        with.Query("token", "1");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.True);
                }

                [Test]
                public void IsTrueForContributor()
                {
                    var result = _browser.Get("/project/1", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "pm");
                        with.Query("token", "4");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.True);
                }

                [Test]
                public void IsFalseForNonContributor()
                {
                    var result = _browser.Get("/project/1", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "pm");
                        with.Query("token", "5");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfUserIsAnynymous()
                {
                    var result = _browser.Get("/project/1", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "anonymous");
                        with.Query("token", "user");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfUserIsPublic()
                {
                    var result = _browser.Get("/project/1", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "public");
                        with.Query("token", "user");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfProjectDoesNotAllowFeaturesPublicRole()
                {
                    var result = _browser.Get("/project/2", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "public");
                        with.Query("token", "user");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfProjectDoesNotAllowFeaturesAnonymousRole()
                {
                    var result = _browser.Get("/project/2", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "anonymous");
                        with.Query("token", "user");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsTrueIfProjectDoesNotAllowFeaturesAdminRole()
                {
                    var result = _browser.Get("/project/2", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "admin");
                        with.Query("token", "user");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.True);
                }

                [Test]
                public void IsFalseIfProjectStatusIsCancelled()
                {
                    var result = _browser.Get("/project/3", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "pm");
                        with.Query("token", "3");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsFalseIfProjectStatusIsComplete()
                {
                    var result = _browser.Get("/project/4", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "pm");
                        with.Query("token", "4");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.False);
                }

                [Test]
                public void IsTrueIfProjectStatusIsCancelledForAdmin()
                {
                    var result = _browser.Get("/project/3", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "admin");
                        with.Query("token", "3");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.True);
                }

                [Test]
                public void IsTrueIfProjectStatusIsCompleteForAdmin()
                {
                    var result = _browser.Get("/project/4", with =>
                    {
                        with.HttpRequest();
                        with.Query("key", "admin");
                        with.Query("token", "4");
                    });

                    var model = result.Body.DeserializeJson<ProjectWithFeaturesResponse>();

                    Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                    Assert.That(model.AllowEdits, Is.True);
                }
            }
        }
    }
}