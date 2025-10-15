using System;
using UnityEngine;

namespace Tetrage.Core.Ids
{
    /// <summary>
    /// 強いID型の定義群。各IDは <see cref="int"/> を内包し、不変・比較可能です。
    /// </summary>
    #region PlayerId
    /// <summary>
    /// プレイヤーの識別子（オンライン時は Photon の ActorNumber を包む想定）
    /// </summary>
    [Serializable]
    public readonly struct PlayerId : IEquatable<PlayerId>
    {
        [SerializeField]
        private readonly int _value;
        /// <summary>不変の整数値</summary>
        public int Value { get { return _value; } }

        public PlayerId(int value)
        {
            _value = value;
        }

        public bool Equals(PlayerId other) => _value == other._value;
        public override bool Equals(object obj) => obj is PlayerId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();

        public static implicit operator int(PlayerId id) => id._value;
        public static explicit operator PlayerId(int value) => new PlayerId(value);
    }
    #endregion

    #region DeckId
    /// <summary>
    /// デッキ（カード集合インスタンス）の識別子
    /// </summary>
    [Serializable]
    public readonly struct DeckId : IEquatable<DeckId>
    {
        [SerializeField]
        private readonly int _value;
        public int Value { get { return _value; } }

        public DeckId(int value)
        {
            _value = value;
        }

        public bool Equals(DeckId other) => _value == other._value;
        public override bool Equals(object obj) => obj is DeckId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();

        public static implicit operator int(DeckId id) => id._value;
        public static explicit operator DeckId(int value) => new DeckId(value);
    }
    #endregion

    #region CardId
    /// <summary>
    /// カードの識別子（例: DeckId × SuitIndex × Number で決定論的に合成）
    /// </summary>
    [Serializable]
    public readonly struct CardId : IEquatable<CardId>
    {
        [SerializeField]
        private readonly int _value;
        public int Value { get { return _value; } }

        public CardId(int value)
        {
            _value = value;
        }

        public bool Equals(CardId other) => _value == other._value;
        public override bool Equals(object obj) => obj is CardId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();

        public static implicit operator int(CardId id) => id._value;
        public static explicit operator CardId(int value) => new CardId(value);
    }
    #endregion

    #region PileId
    /// <summary>
    /// カード置き場（山札/手札/捨て札/ターゲットなど）の識別子
    /// </summary>
    [Serializable]
    public readonly struct PileId : IEquatable<PileId>
    {
        [SerializeField]
        private readonly int _value;
        public int Value { get { return _value; } }

        public PileId(int value)
        {
            _value = value;
        }

        public bool Equals(PileId other) => _value == other._value;
        public override bool Equals(object obj) => obj is PileId other && Equals(other);
        public override int GetHashCode() => _value;
        public override string ToString() => _value.ToString();

        public static implicit operator int(PileId id) => id._value;
        public static explicit operator PileId(int value) => new PileId(value);
    }
    #endregion
}


