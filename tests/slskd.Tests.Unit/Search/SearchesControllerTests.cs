namespace slskd.Tests.Unit.Search
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Options;
    using Moq;
    using slskd.Search;
    using slskd.Search.API;
    using Xunit;

    public class SearchesControllerTests
    {
        public class GetResponsesById
        {
            [Fact]
            public async Task Returns_Responses_Received_So_Far_While_Search_Is_In_Progress()
            {
                var id = Guid.NewGuid();
                IEnumerable<Response> inProgress = new List<Response> { new Response { Username = "a" }, new Response { Username = "b" } };

                var (controller, searches) = GetFixture();
                searches.Setup(s => s.TryGetResponsesInProgress(id, out inProgress)).Returns(true);

                var result = await controller.GetResponsesById(id);

                var ok = Assert.IsType<OkObjectResult>(result);
                Assert.Same(inProgress, ok.Value);
                searches.Verify(s => s.FindAsync(It.IsAny<Expression<Func<Search, bool>>>(), It.IsAny<bool>()), Times.Never);
            }

            [Fact]
            public async Task Returns_Persisted_Responses_When_Search_Is_Not_In_Progress()
            {
                var id = Guid.NewGuid();
                IEnumerable<Response> none = null;
                var persisted = new List<Response> { new Response { Username = "c" } };

                var (controller, searches) = GetFixture();
                searches.Setup(s => s.TryGetResponsesInProgress(id, out none)).Returns(false);
                searches.Setup(s => s.FindAsync(It.IsAny<Expression<Func<Search, bool>>>(), true))
                    .ReturnsAsync(new Search { Id = id, Responses = persisted });

                var result = await controller.GetResponsesById(id);

                var ok = Assert.IsType<OkObjectResult>(result);
                Assert.Same(persisted, ok.Value);
            }

            [Fact]
            public async Task Returns_NotFound_When_Search_Does_Not_Exist()
            {
                var id = Guid.NewGuid();
                IEnumerable<Response> none = null;

                var (controller, searches) = GetFixture();
                searches.Setup(s => s.TryGetResponsesInProgress(id, out none)).Returns(false);
                searches.Setup(s => s.FindAsync(It.IsAny<Expression<Func<Search, bool>>>(), true))
                    .ReturnsAsync((Search)null);

                var result = await controller.GetResponsesById(id);

                Assert.IsType<NotFoundResult>(result);
            }
        }

        private static (SearchesController Controller, Mock<ISearchService> Searches) GetFixture()
        {
            var searches = new Mock<ISearchService>();
            var options = new Mock<IOptionsSnapshot<slskd.Options>>();
            options.Setup(o => o.Value).Returns(new slskd.Options());

            return (new SearchesController(searches.Object, options.Object), searches);
        }
    }
}
