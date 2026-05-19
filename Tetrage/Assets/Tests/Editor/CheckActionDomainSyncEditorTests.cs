using NUnit.Framework;
using Tetrage.Core.Events;
using Tetrage.Core.Ids;
using Tetrage.Models;
using Tetrage.Network.Gameplay;
using UnityEngine;
using R3;

namespace Tetrage.Tests.Editor
{
  /// <summary>
  /// Check 成功時の IsSuitVisible 同期に関するテスト。
  /// </summary>
  public class CheckActionDomainSyncEditorTests
  {
    [Test]
    public void CardStateChangedEventPacket_JsonUtility_RoundTripsIsSuitVisibleStateCode()
    {
      var packet = new CardStateChangedEventPacket
      {
        sequence = 1,
        stateVersion = 1,
        cardId = 42,
        stateCode = CardStateCode.IsSuitVisible,
        stateValue = true,
      };

      var json = JsonUtility.ToJson(packet);
      var roundTrip = JsonUtility.FromJson<CardStateChangedEventPacket>(json);

      Assert.AreEqual(CardStateCode.IsSuitVisible, roundTrip.stateCode);
      Assert.IsTrue(roundTrip.stateValue);
    }

    [Test]
    public void ToDomain_CardStateChanged_IsSuitVisible_MapsStateType()
    {
      var converter = new DomainEventConverter(new PlayerIdMapper());
      var dto = new CardStateChangedEventPacket
      {
        sequence = 3,
        cardId = 99,
        stateCode = CardStateCode.IsSuitVisible,
        stateValue = true,
      };

      var domain = converter.ToDomain(dto);

      Assert.AreEqual(CardStateType.IsSuitVisible, domain.StateType);
      Assert.IsTrue(domain.StateValue);
      Assert.AreEqual(new CardId(99), domain.CardId);
    }

    [Test]
    public void SetSuitVisible_OnCard_NotifiesPresenter()
    {
      var card = new Card(new CardId(1), Tetrage.Core.Enums.Suit.Heart, 1, isFaceUp: false);
      bool notified = false;
      card.CardChanged.Subscribe(_ => notified = true);

      card.SetSuitVisible(true);

      Assert.IsTrue(card.IsSuitVisible);
      Assert.IsTrue(notified);
    }
  }
}
