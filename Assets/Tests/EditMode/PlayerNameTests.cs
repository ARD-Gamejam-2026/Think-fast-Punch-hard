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

        [Test]
        public void Is_set_is_false_when_unset_blank_or_anon()
        {
            PlayerPrefs.DeleteKey("thinkfast.playerName");
            Assert.IsFalse(PlayerName.IsSet);

            PlayerName.Set("   ");
            Assert.IsFalse(PlayerName.IsSet);

            PlayerName.Set("anon");
            Assert.IsFalse(PlayerName.IsSet);
        }

        [Test]
        public void Is_set_is_true_after_a_real_name()
        {
            PlayerName.Set("Marcel");
            Assert.IsTrue(PlayerName.IsSet);
        }
    }
}
