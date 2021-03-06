using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using LanguageExt;
using Moq;
using Xunit;
using Unit = System.Reactive.Unit;

namespace ExRam.Gremlinq.Tests
{
    public class GremlinQueryProviderTest
    {
        [Fact]
        public void WithSubgraphStrategyTest()
        {
            var queryProviderMock = new Mock<ITypedGremlinQueryProvider>();
            var subgraphStrategyProvider = queryProviderMock.Object.WithSubgraphStrategy(_ => _.OfType<User>(), _ => _);

            subgraphStrategyProvider
                .Execute(GremlinQuery.Create("g").Cast<Unit>());

            queryProviderMock.Verify(x => x.Execute(It.Is<IGremlinQuery<Unit>>(query => query.Steps[0] is TerminalGremlinStep && ((TerminalGremlinStep)query.Steps[0]).Name == "withStrategies" && ((TerminalGremlinStep)query.Steps[0]).Parameters.Count == 1)));
        }

        [Fact]
        public void WithSubgraphStrategy_is_ommitted_if_empty()
        {
            var queryProviderMock = new Mock<ITypedGremlinQueryProvider>();
            var subgraphStrategyProvider = queryProviderMock.Object.WithSubgraphStrategy(_ => _, _ => _);

            subgraphStrategyProvider
                .Execute(GremlinQuery.Create("g").Cast<Unit>());

            queryProviderMock.Verify(x => x.Execute(It.Is<IGremlinQuery<Unit>>(query => query.Steps.Count == 0)));
        }

        [Fact]
        public void RewriteStepsTest()
        {
            var queryProviderMock = new Mock<ITypedGremlinQueryProvider>();
            var subgraphStrategyProvider = queryProviderMock.Object.RewriteSteps<AddElementPropertiesStep>(
                step =>
                {
                    return step.Element is User
                        ? new[] { new ReplaceElementPropertyStep<User, int>(step, user => user.Age, 36) }
                        : Option<IEnumerable<GremlinStep>>.None;
                });

            subgraphStrategyProvider
                .Execute(GremlinQuery.Create("g").AddV(new User()));

            queryProviderMock.Verify(x => x.Execute(It.Is<IGremlinQuery<User>>(query => query.Steps[1] is ReplaceElementPropertyStep<User, int>)));
        }

        [Fact]
        public async Task Scalar()
        {
            var queryProviderMock = new Mock<INativeGremlinQueryProvider>();
            queryProviderMock
                .Setup(x => x.Execute(It.IsAny<string>(), It.IsAny<IDictionary<string, object>>()))
                .Returns(AsyncEnumerable.Return("36"));

            var value = await queryProviderMock.Object
                .WithModel(GraphModel.FromAssembly(Assembly.GetExecutingAssembly(), typeof(Vertex), typeof(Edge), GraphElementNamingStrategy.Simple))
                .WithJsonSupport()
                .Execute(GremlinQuery.Create("g").Cast<int>())
                .First();

            value.Should().Be(36);
        }
    }
}