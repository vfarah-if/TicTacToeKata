﻿using System;
using AutoFixture;
using AutoFixture.AutoMoq;
using AutoFixture.NUnit3;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace Codurance.Domain.Unit.Tests
{
    [TestFixture]
    public class WhenPlayingTickTackToe
    {
        private IFixture fixture;
        private Mock<IBoard> mockBoard;
        private IPlayer playerOne;
        private IPlayer playerTwo;

        [OneTimeSetUp]
        public void OneTimeSetUpAttribute()
        {
            fixture = new Fixture().Customize(new AutoMoqCustomization());
            mockBoard = fixture.Freeze<Mock<IBoard>>();
        }

        [TearDown]
        public void Teardown()
        {
            mockBoard.Reset();
        }

        [SetUp]
        public void SetupTwoPlayersAndAnActivePlayer()
        {
            playerOne = CreatePlayer(Team.Cross);
            playerTwo = CreatePlayer(Team.Zero);
            mockBoard.Setup(x => x.PlayerOne).Returns(playerOne);
            mockBoard.Setup(x => x.PlayerTwo).Returns(playerTwo);
            mockBoard.Setup(x => x.ActivePlayer).Returns(playerTwo);
        }

        private IPlayer CreatePlayer(Team team)
        {
            return new Player(team, fixture.Create<bool>(), fixture.Create<string>());
        }

        [TestFixture]
        public class AndResettingTheGame : WhenPlayingTickTackToe
        {
            [TestCase(Team.Cross)]
            [TestCase(Team.Zero)]
            public void ShouldResetTheBoardWithPlayerOneSetAsCross(Team playerOneTeam)
            {
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();

                subject.Reset(playerOneTeam);

                mockBoard.Verify(x => x.Reset(It.Is<Team>(playerOne => playerOne == playerOneTeam)), Times.Once);
            }
        }

        [TestFixture]
        public class AndMakingAMove : WhenPlayingTickTackToe
        {
            [Test, AutoData]
            public void ShouldUseTheBoardToMakeAMove(BoardPosition position)
            {
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();

                subject.Move(position);

                mockBoard.Verify(x => x.Move(It.Is<BoardPosition>(pos => pos == position)), Times.Once);
            }
        }

        [TestFixture]
        public class AndKeepingTheWinningDetails : WhenPlayingTickTackToe
        {
            [Test]
            public void ShouldStartTheScoreOnZeroZeroForBothTeams()
            {
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();

                subject.Scores[Team.Zero].Should().Be(0);
                subject.Scores[Team.Cross].Should().Be(0);
            }

            [Test]
            public void ShouldIncrementTheScoreWhenATeamWins()
            {
                Action<GameFinishedArgs> gameFinished = null;
                // Cant get it to work with new methods because of
                // https://github.com/moq/moq4/issues/218
#pragma warning disable CS0618 // Type or member is obsolete
                mockBoard.SetupSet(x => x.GameFinished).Callback((x) =>
#pragma warning restore CS0618 // Type or member is obsolete
                {
                    gameFinished = x;
                });
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();

                gameFinished?.Invoke(new GameFinishedArgs(GameResult.Win, mockBoard.Object.PlayerOne, LineWin.RightDiagonal));

                subject.Scores[mockBoard.Object.PlayerOne.Team].Should().Be(1);
            }

            [Test]
            public void ShouldShowWhichTeamAndLineWonAndThatTheGameIsOver()
            {
                Action<GameFinishedArgs> gameFinished = null;
                Team actualWinningTeam = Team.None;
                LineWin actualLineWin = LineWin.None;
#pragma warning disable CS0618
                mockBoard.SetupSet(x => x.GameFinished).Callback((x) =>
#pragma warning restore CS0618
                {
                    gameFinished = x;
                });
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();
                subject.TeamAndLineWon += (ValueTuple<Team, LineWin> arg) =>
                {
                    actualWinningTeam = arg.Item1;
                    actualLineWin = arg.Item2;
                };
                subject.IsGameOver.Should().BeFalse();

                gameFinished?.Invoke(new GameFinishedArgs(GameResult.Win, mockBoard.Object.PlayerOne, LineWin.MiddleHorizontal));

                actualWinningTeam.Should().Be(mockBoard.Object.PlayerOne.Team);
                actualLineWin.Should().Be(LineWin.MiddleHorizontal);
                subject.IsGameOver.Should().BeTrue();
            }

            [Test]
            public void ShouldNotIncrementTheScoreWhenThereIsADraw()
            {
                Action<GameFinishedArgs> gameFinished = null;
                Team actualWinningTeam = Team.None;
                LineWin actualLineWin = LineWin.None;
#pragma warning disable CS0618
                mockBoard.SetupSet(x => x.GameFinished).Callback((x) =>
#pragma warning restore CS0618
                {
                    gameFinished = x;
                });
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();
                subject.TeamAndLineWon += (ValueTuple<Team, LineWin> arg) =>
                {
                    actualWinningTeam = arg.Item1;
                    actualLineWin = arg.Item2;
                };
                subject.IsGameOver.Should().BeFalse();

                gameFinished?.Invoke(new GameFinishedArgs(GameResult.Draw, null, LineWin.None));

                subject.Scores[mockBoard.Object.PlayerOne.Team].Should().Be(0);
                subject.Scores[mockBoard.Object.PlayerTwo.Team].Should().Be(0);
                actualWinningTeam.Should().Be(Team.None);
                actualLineWin.Should().Be(LineWin.None);
                subject.IsGameOver.Should().BeTrue();
            }
        }

        [TestFixture]
        public class AndSwappingPlayers : WhenPlayingTickTackToe
        {
            [Test]
            public void ShouldSwapToWhichEverTeamIsActivated()
            {
                Team activeTeam = Team.None;
                Action<PlayerSwappedArgs> playerSwapped = null;
#pragma warning disable CS0618 
                mockBoard.SetupSet(x => x.PlayerSwapped).Callback((x) =>
#pragma warning restore CS0618 
                {
                    playerSwapped = x;
                });
                ITicTacToeGame subject = fixture.Create<TicTacToeGame>();
                subject.TeamChanged += (Team team) =>
                {
                    activeTeam = team;
                };

                playerSwapped?.Invoke(new PlayerSwappedArgs(playerOne));

                activeTeam.Should().Be(playerOne.Team);
            }
        }
    }
}

