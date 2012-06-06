﻿namespace System.Data.Entity.Core.Mapping.Update.Internal
{
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.Entity.Core.Objects;
    using System.Data.Entity.Resources;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Moq;
    using Moq.Protected;
    using Xunit;

    public class UpdateTranslatorTests
    {
        public class Update
        {
            [Fact]
            private void Propagates_server_gen_values_and_returns_entities_affected()
            {
                var mockUpdateTranslator = new Mock<UpdateTranslator>()
                                               {
                                                   CallBase = true
                                               };

                int serverGenValuesPropagatedCount = 0;
                var generatedValue = new object();
                var mockPropagatorResult = new Mock<PropagatorResult>()
                                               {
                                                   CallBase = true
                                               };
                mockPropagatorResult.Setup(m => m.SetServerGenValue(It.IsAny<object>()))
                    .Callback<object>(
                        o =>
                        {
                            serverGenValuesPropagatedCount++;
                            Assert.Same(generatedValue, o);
                        });

                var mockUpdateCommand = new Mock<UpdateCommand>(
                    mockUpdateTranslator.Object,
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0),
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0));
                mockUpdateCommand.Setup(
                    m => m.Execute(It.IsAny<Dictionary<int, object>>(), It.IsAny<List<KeyValuePair<PropagatorResult, object>>>()))
                    .Returns(
                        (Dictionary<int, object> identifierValues, List<KeyValuePair<PropagatorResult, object>> generatedValues) =>
                        {
                            generatedValues.Add(
                                new KeyValuePair<PropagatorResult, object>(mockPropagatorResult.Object, generatedValue));
                            return 1;
                        });

                mockUpdateTranslator.Protected().Setup<IEnumerable<UpdateCommand>>("ProduceCommands").Returns(
                    new[] { mockUpdateCommand.Object });

                var entitiesAffected = mockUpdateTranslator.Object.Update();

                Assert.Equal(0, entitiesAffected);
                Assert.Equal(1, serverGenValuesPropagatedCount);
            }

            [Fact]
            public void Wraps_exceptions()
            {
                var mockUpdateTranslator = new Mock<UpdateTranslator>()
                                               {
                                                   CallBase = true
                                               };

                var dbException = new Mock<DbException>("Exception message").Object;

                var mockUpdateCommand = new Mock<UpdateCommand>(
                    mockUpdateTranslator.Object,
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0),
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0));
                mockUpdateCommand.Setup(
                    m => m.Execute(It.IsAny<Dictionary<int, object>>(), It.IsAny<List<KeyValuePair<PropagatorResult, object>>>()))
                    .Returns(
                        (Dictionary<int, object> identifierValues, List<KeyValuePair<PropagatorResult, object>> generatedValues) =>
                        {
                            throw dbException;
                        });

                var objectStateManager = new ObjectStateManager(
                    new Mock<InternalObjectStateManager>()
                        {
                            CallBase = true
                        }.Object);
                var mockObjectStateEntry = new Mock<ObjectStateEntry>(objectStateManager, /*entitySet:*/null, EntityState.Unchanged);

                mockUpdateCommand.Setup(m => m.GetStateEntries(It.IsAny<UpdateTranslator>()))
                    .Returns(new[] { mockObjectStateEntry.Object });

                new List<KeyValuePair<PropagatorResult, object>>();
                mockUpdateTranslator.Protected().Setup<IEnumerable<UpdateCommand>>("ProduceCommands").Returns(
                    new[] { mockUpdateCommand.Object });

                var exception = Assert.Throws<UpdateException>(() => mockUpdateTranslator.Object.Update());
                Assert.Equal(Strings.Update_GeneralExecutionException, exception.Message);
                Assert.Same(dbException, exception.InnerException);
                Assert.Same(mockObjectStateEntry.Object, exception.StateEntries.Single());
            }
        }

        public class UpdateAsync
        {
            [Fact]
            private void Propagates_server_gen_values_and_returns_entities_affected()
            {
                var mockUpdateTranslator = new Mock<UpdateTranslator>()
                {
                    CallBase = true
                };

                int serverGenValuesPropagatedCount = 0;
                var generatedValue = new object();
                var mockPropagatorResult = new Mock<PropagatorResult>()
                {
                    CallBase = true
                };
                mockPropagatorResult.Setup(m => m.SetServerGenValue(It.IsAny<object>()))
                    .Callback<object>(
                        o =>
                        {
                            serverGenValuesPropagatedCount++;
                            Assert.Same(generatedValue, o);
                        });

                var mockUpdateCommand = new Mock<UpdateCommand>(
                    mockUpdateTranslator.Object,
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0),
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0));

                mockUpdateCommand.Setup(
                    m => m.ExecuteAsync(It.IsAny<Dictionary<int, object>>(),
                        It.IsAny<List<KeyValuePair<PropagatorResult, object>>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(
                        (Dictionary<int, object> identifierValues,
                            List<KeyValuePair<PropagatorResult, object>> generatedValues,
                            CancellationToken cancellationToken) =>
                        {
                            generatedValues.Add(
                                new KeyValuePair<PropagatorResult, object>(mockPropagatorResult.Object, generatedValue));
                            return Task.FromResult(1L);
                        });

                mockUpdateTranslator.Protected().Setup<IEnumerable<UpdateCommand>>("ProduceCommands").Returns(
                    new[] { mockUpdateCommand.Object });

                var entitiesAffected = mockUpdateTranslator.Object.UpdateAsync(CancellationToken.None).Result;

                Assert.Equal(0, entitiesAffected);
                Assert.Equal(1, serverGenValuesPropagatedCount);
            }

            [Fact]
            public void Wraps_exceptions()
            {
                var mockUpdateTranslator = new Mock<UpdateTranslator>()
                {
                    CallBase = true
                };

                var dbException = new Mock<DbException>("Exception message").Object;

                var mockUpdateCommand = new Mock<UpdateCommand>(
                    mockUpdateTranslator.Object,
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0),
                    PropagatorResult.CreateSimpleValue(PropagatorFlags.NoFlags, value: 0));
                mockUpdateCommand.Setup(
                    m => m.ExecuteAsync(It.IsAny<Dictionary<int, object>>(),
                        It.IsAny<List<KeyValuePair<PropagatorResult, object>>>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(
                        (Dictionary<int, object> identifierValues, 
                         List<KeyValuePair<PropagatorResult, object>> generatedValues,
                         CancellationToken cancellationToken) =>
                        {
                            throw dbException;
                        });

                var objectStateManager = new ObjectStateManager(
                    new Mock<InternalObjectStateManager>()
                    {
                        CallBase = true
                    }.Object);
                var mockObjectStateEntry = new Mock<ObjectStateEntry>(objectStateManager, /*entitySet:*/null, EntityState.Unchanged);

                mockUpdateCommand.Setup(m => m.GetStateEntries(It.IsAny<UpdateTranslator>()))
                    .Returns(new[] { mockObjectStateEntry.Object });

                new List<KeyValuePair<PropagatorResult, object>>();
                mockUpdateTranslator.Protected().Setup<IEnumerable<UpdateCommand>>("ProduceCommands").Returns(
                    new[] { mockUpdateCommand.Object });

                var exception = Assert.Throws<AggregateException>(() => mockUpdateTranslator.Object.UpdateAsync(CancellationToken.None).Result);
                Assert.IsType<UpdateException>(exception.InnerException);
                Assert.Equal(Strings.Update_GeneralExecutionException, exception.InnerException.Message);
                Assert.Same(dbException, exception.InnerException.InnerException);
                Assert.Same(mockObjectStateEntry.Object, ((UpdateException)exception.InnerException).StateEntries.Single());
            }
        }
    }
}