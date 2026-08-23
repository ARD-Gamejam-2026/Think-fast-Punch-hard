using NUnit.Framework;
using UnityEngine;
using ThinkFast.Stats;

namespace ThinkFast.Stats.Tests
{
    public class PlayerNameTests
    {
        [TearDown]
        public void Clear()
        {
            PlayerPrefs.DeleteKey("thinkfast.playerName");
        }

        [Test]
        public void Defaults_to_anon_when_unset()
        {
            PlayerPrefs.DeleteKey("thinkfast.playerName");
            Assert.AreEqual("anon", PlayerName.Value);
        }

        [Test]
        public void Set_then_read_round_trips_the_name()
        {
            PlayerName.Set("Marcel");
            Assert.AreEqual("Marcel", PlayerName.Value);
        }

        [Test]
        public void Set_blank_falls_back_to_anon()
        {
            PlayerName.Set("   ");
            Assert.AreEqual("anon", PlayerName.Value);
        }
    }
}
