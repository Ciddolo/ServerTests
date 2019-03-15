using System;
using System.Text;
using System.Net;
using NUnit.Framework;
using System.Collections.Generic;

namespace GameServerExample2B.Test
{
    public class TestGameServer
    {
        private Dictionary<uint, GameServer> serversTable;

        private Packet join = new Packet(0);
        private Packet quit = new Packet(254);

        private FakeTransport transport0;
        private FakeClock clock0;
        private GameServer server0;

        private FakeTransport transport1;
        private FakeClock clock1;
        private GameServer server1;

        private FakeTransport transport2;
        private FakeClock clock2;
        private GameServer server2;

        [SetUp]
        public void SetupTests()
        {
            serversTable = new Dictionary<uint, GameServer>();

            transport0 = new FakeTransport();
            clock0 = new FakeClock();
            server0 = new GameServer(transport0, clock0);
            serversTable.Add(0, server0);

            transport1 = new FakeTransport();
            clock1 = new FakeClock();
            server1 = new GameServer(transport1, clock1);
            serversTable.Add(1, server1);

            transport2 = new FakeTransport();
            clock2 = new FakeClock();
            server2 = new GameServer(transport2, clock2);
            serversTable.Add(2, server2);
        }

        [Test]
        public void TestZeroNow()
        {
            Assert.That(server0.Now, Is.EqualTo(0));
            Assert.That(server1.Now, Is.EqualTo(0));
            Assert.That(server2.Now, Is.EqualTo(0));
        }

        [Test]
        public void TestSpecificNow()
        {
            clock0.IncreaseTimeStamp(10.0f);
            server0.SingleStep();

            clock1.IncreaseTimeStamp(20.0f);
            server1.SingleStep();

            clock2.IncreaseTimeStamp(30.0f);
            server2.SingleStep();

            Assert.That(server0.Now, Is.EqualTo(10.0f));
            Assert.That(server1.Now, Is.EqualTo(20.0f));
            Assert.That(server2.Now, Is.EqualTo(30.0f));
        }

        [Test]
        public void TestNotNegativeNow()
        {
            clock0.IncreaseTimeStamp(10.0f);
            server0.SingleStep();

            clock1.IncreaseTimeStamp(20.0f);
            server1.SingleStep();

            clock2.IncreaseTimeStamp(30.0f);
            server2.SingleStep();

            Assert.That(server0.Now, Is.AtLeast(0.0f));
            Assert.That(server1.Now, Is.AtLeast(0.0f));
            Assert.That(server2.Now, Is.AtLeast(0.0f));
        }

        [Test]
        public void TestClientsOnStart()
        {
            Assert.That(server0.NumClients, Is.EqualTo(0));
            Assert.That(server1.NumClients, Is.EqualTo(0));
            Assert.That(server2.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestGameObjectsOnStart()
        {
            Assert.That(server0.NumGameObjects, Is.EqualTo(0));
            Assert.That(server1.NumGameObjects, Is.EqualTo(0));
            Assert.That(server2.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestJoinNumOfClients()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinNumOfGameObjects()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestWelcomeAfterJoin()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            FakeData welcome = transport0.ClientDequeue();

            Assert.That(welcome.data[0], Is.EqualTo(1));
        }

        [Test]
        public void TestDequeueEmptyQueue()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientDequeue();

            Assert.That(() => transport0.ClientDequeue(), Throws.InstanceOf<FakeQueueEmpty>());
        }

        [Test]
        public void TestJoinSameClient()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestJoinTwoDifferentClient()
        {
            transport0.ClientEnqueue(join, "tester0", 0);
            transport0.ClientEnqueue(join, "tester1", 0);

            server0.SingleStep();
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressClient()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "tester", 1);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinSameAddressAvatars()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "tester", 1);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsSamePort()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "foobar", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(2));
        }

        [Test]
        public void TestJoinTwoClientsWelcome()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "foobar", 1);
            server0.SingleStep();

            Assert.That(transport0.ClientQueueCount, Is.EqualTo(5));
            Assert.That(transport0.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport0.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport0.ClientDequeue().endPoint.Address, Is.EqualTo("tester"));
            Assert.That(transport0.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
            Assert.That(transport0.ClientDequeue().endPoint.Address, Is.EqualTo("foobar"));
        }

        [Test]
        public void TestEvilUpdate()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            uint testerId = BitConverter.ToUInt32(transport0.ClientDequeue().data, 5);

            transport0.ClientEnqueue(join, "foobar", 1);
            server0.SingleStep();

            Packet move = new Packet(3, testerId, 10.0f, 20.0f, 30.0f);
            transport0.ClientEnqueue(move, "foobar", 1);
            server0.SingleStep();

            Assert.That(server0.GetGameObject(testerId).X, Is.Not.EqualTo(10.0f));
            Assert.That(server0.GetGameObject(testerId).Y, Is.Not.EqualTo(20.0f));
            Assert.That(server0.GetGameObject(testerId).Z, Is.Not.EqualTo(30.0f));
            // TODO get the id from the welcome packets
            // try to move the id from the other player
        }

        [Test]
        public void TestGoodUpdate()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            uint testerId = BitConverter.ToUInt32(transport0.ClientDequeue().data, 5);

            Packet move = new Packet(3, testerId, 10.0f, 20.0f, 30.0f);
            transport0.ClientEnqueue(move, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.GetGameObject(testerId).X, Is.EqualTo(10.0f));
            Assert.That(server0.GetGameObject(testerId).Y, Is.EqualTo(20.0f));
            Assert.That(server0.GetGameObject(testerId).Z, Is.EqualTo(30.0f));
        }

        [Test]
        public void TestNumberOfServers()
        {
            Assert.That(serversTable.Count, Is.EqualTo(3.0f));
        }

        [Test]
        public void TestStartingMalus()
        {
            GameClient client = new GameClient(server0, transport0.CreateEndPoint());
            Assert.That(client.Malus, Is.EqualTo(0));
        }

        [Test]
        public void TestEvilUpdateMalus()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            //uint testerId = BitConverter.ToUInt32(transport0.ClientDequeue().data, 5);
            FakeData testerData = transport0.ClientDequeue();
            uint testerId = BitConverter.ToUInt32(testerData.data, 5);
            EndPoint testerEndPoint = testerData.endPoint;

            transport0.ClientEnqueue(join, "foobar", 1);
            server0.SingleStep();
            transport0.ClientDequeue();

            uint foobarId = BitConverter.ToUInt32(transport0.ClientDequeue().data, 5);

            Packet move = new Packet(3, foobarId, 10.0f, 20.0f, 30.0f);
            transport0.ClientEnqueue(move, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.GetGameClient(testerEndPoint).Malus, Is.EqualTo(10));
        }

        [Test]
        public void TestDoubleJoinMalus()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.GetGameClients()[0].Malus, Is.EqualTo(1));
        }

        [Test]
        public void TestNumberOfClientsOnServer0()
        {
            transport0.ClientEnqueue(join, "tester0", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
            Assert.That(server1.NumClients, Is.EqualTo(0));
            Assert.That(server2.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestNumberOfClientsOnServer0And1()
        {
            transport0.ClientEnqueue(join, "tester0", 0);
            server0.SingleStep();

            transport1.ClientEnqueue(join, "tester0", 0);
            server1.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
            Assert.That(server1.NumClients, Is.EqualTo(1));
            Assert.That(server2.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestNumberOfClientsOnAllServer()
        {
            transport0.ClientEnqueue(join, "tester0", 0);
            server0.SingleStep();

            transport1.ClientEnqueue(join, "tester0", 0);
            server1.SingleStep();

            transport2.ClientEnqueue(join, "tester0", 0);
            server2.SingleStep();
            
            Assert.That(server0.NumClients, Is.EqualTo(1));
            Assert.That(server1.NumClients, Is.EqualTo(1));
            Assert.That(server2.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestClientQuit()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));

            transport0.ClientEnqueue(quit, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(0));
        }

        [Test]
        public void TestAvatarQuit()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(1));

            transport0.ClientEnqueue(quit, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(0));
        }

        [Test]
        public void TestClientJoinQuitJoin()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));

            transport0.ClientEnqueue(quit, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(0));

            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
        }

        [Test]
        public void TestAvatarJoinQuitJoin()
        {
            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(1));

            transport0.ClientEnqueue(quit, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(0));

            transport0.ClientEnqueue(join, "tester", 0);
            server0.SingleStep();

            Assert.That(server0.NumGameObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestClientQuitException()
        {
            transport0.ClientEnqueue(quit, "tester", 0);
            server0.SingleStep();

            Assert.That(() => server0.SingleStep(), Throws.Exception);
        }

        [Test]
        public void TestClientMultipleQuit()
        {
            transport0.ClientEnqueue(join, "tester0", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "tester1", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(join, "tester2", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(3));

            transport0.ClientEnqueue(quit, "tester1", 0);
            server0.SingleStep();

            transport0.ClientEnqueue(quit, "tester2", 0);
            server0.SingleStep();

            Assert.That(server0.NumClients, Is.EqualTo(1));
        }
    }
}
