﻿using GitHub;
using NSubstitute;
using System;
using UnitTests;
using Xunit;

/// <summary>
/// Tests all of the conditions that 
/// </summary>
public class ThrownTests
{
    [Fact]
    public void CompareOperation()
    {
        const int expectedResult = 42;

        var mock = Substitute.For<IControlCandidate<int>>();
        mock.Control().Returns(expectedResult);
        mock.Candidate().Returns(0);

        var ex = new Exception();

        var result = Scientist.Science<int>(nameof(CompareOperation), experiment =>
        {
            experiment.Thrown(mock.Thrown);
            experiment.Compare((x, y) =>
            {
                throw ex;
            });
            experiment.Use(mock.Control);
            experiment.Try(mock.Candidate);
        });

        Assert.Equal(expectedResult, result);
        mock.Received().Thrown(Operation.Compare, ex);
    }

    [Fact]
    public void IgnoreOperation()
    {
        const int expectedResult = 42;

        var mock = Substitute.For<IControlCandidate<int>>();
        mock.Control().Returns(expectedResult);
        mock.Candidate().Returns(0);

        var ex = new Exception();

        var result = Scientist.Science<int>(nameof(IgnoreOperation), experiment =>
        {
            experiment.Thrown(mock.Thrown);
            experiment.Ignore((x, y) =>
            {
                throw ex;
            });
            experiment.Use(mock.Control);
            experiment.Try(mock.Candidate);
        });

        Assert.Equal(expectedResult, result);
        mock.Received().Thrown(Operation.Ignore, ex);
    }

    [Fact]
    public void PublishOperation()
    {
        var publisher = Substitute.For<IResultPublisher>();
        var ex = new Exception();
        publisher.Publish(Arg.Any<Result<int>>()).Throws(ex);

        using (new SwapPublisher(publisher))
        {
            const int expectedResult = 42;

            var mock = Substitute.For<IControlCandidate<int>>();
            mock.Control().Returns(expectedResult);
            mock.Candidate().Returns(0);

            var result = Scientist.Science<int>(nameof(PublishOperation), experiment =>
            {
                experiment.Thrown(mock.Thrown);
                experiment.Use(mock.Control);
                experiment.Try(mock.Candidate);
            });

            Assert.Equal(expectedResult, result);
            mock.Received().Thrown(Operation.Publish, ex);
        }
    }

    [Fact]
    public void RunIfOperation()
    {
        const int expectedResult = 42;

        var mock = Substitute.For<IControlCandidate<int>>();
        mock.Control().Returns(expectedResult);
        mock.Candidate().Returns(0);

        var ex = new Exception();

        var result = Scientist.Science<int>(nameof(RunIfOperation), experiment =>
        {
            experiment.Thrown(mock.Thrown);
            experiment.RunIf(() =>
            {
                throw ex;
            });
            experiment.Use(mock.Control);
            experiment.Try(mock.Candidate);
        });

        Assert.Equal(expectedResult, result);
        mock.Received().Thrown(Operation.RunIf, ex);
    }

    /// <summary>
    /// Swaps <see cref="Scientist.ResultPublisher"/> with the input
    /// parameter, and upon disposal exchanges the publisher back.
    /// </summary>
    public class SwapPublisher : IDisposable
    {
        readonly IResultPublisher _original;

        public SwapPublisher(IResultPublisher publisher)
        {
            _original = Scientist.ResultPublisher;
            Scientist.ResultPublisher = publisher;
        }

        public void Dispose()
        {
            Scientist.ResultPublisher = _original;
        }
    }
}
