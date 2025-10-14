using System;
using System.Collections.Generic;

namespace Tetrage.Core.Contracts
{
    /// <summary>
    /// 一意IDでエンティティを参照するためのレジストリ
    /// </summary>
    public sealed class IdRegistry<TId, TEntity> where TEntity : IIdentifiable<TId>
    {
        #region フィールド
        private readonly Dictionary<TId, TEntity> _map = new();
        #endregion

        #region 登録/取得
        /// <summary>
        /// エンティティを登録する（同一IDが存在する場合は上書き）
        /// </summary>
        public void Register(TEntity entity)
        {
            _map[entity.Id] = entity;
        }

        /// <summary>
        /// IDからエンティティを取得する（存在しない場合はfalse）
        /// </summary>
        public bool TryGet(TId id, out TEntity entity) => _map.TryGetValue(id, out entity);

        /// <summary>
        /// IDからエンティティを取得する（存在しない場合は例外）
        /// </summary>
        public TEntity GetOrThrow(TId id)
        {
            if (_map.TryGetValue(id, out var e)) return e;
            throw new KeyNotFoundException($"Id not found: {id}");
        }

        /// <summary>
        /// 登録を解除する（存在しない場合はfalse）
        /// </summary>
        public bool Unregister(TId id) => _map.Remove(id);

        /// <summary>
        /// すべての登録をクリアする
        /// </summary>
        public void Clear() => _map.Clear();

        /// <summary>
        /// 指定IDが登録済みかどうか
        /// </summary>
        public bool Contains(TId id) => _map.ContainsKey(id);

        /// <summary>
        /// 登録件数
        /// </summary>
        public int Count => _map.Count;
        #endregion
    }
}