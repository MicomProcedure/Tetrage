using System.Collections.Generic;
using Tetrage.Core.Contracts;

namespace Tetrage.Core.Ids
{
    /// <summary>
    /// 一意IDでエンティティを参照するためのレジストリ
    /// </summary>
    public sealed class IdRegistry<TId, TEntity> where TEntity : IIdentifiable<TId>
    {
        private readonly Dictionary<TId, TEntity> _map = new();

        /// <summary>
        /// 一意の識別子を持つオブジェクトを登録する
        /// </summary>
        /// <param name="entity">登録するオブジェクト</param>
        public void Register(TEntity entity) => _map[entity.Id] = entity;

        /// <summary>
        /// 一意の識別子を持つオブジェクトを取得する
        /// </summary>
        /// <param name="id">識別子</param>
        /// <param name="entity">取得するオブジェクト</param>
        /// <returns>取得できたかどうか</returns>
        public bool TryGet(TId id, out TEntity entity) => _map.TryGetValue(id, out entity);

        /// <summary>
        /// 一意の識別子を持つオブジェクトを取得する
        /// </summary>
        /// <param name="id">識別子</param>
        /// <returns>取得するオブジェクト</returns>
        /// <exception cref="KeyNotFoundException">識別子が見つからない場合</exception>
        public TEntity GetOrThrow(TId id) => _map.TryGetValue(id, out var e) ? e : throw new KeyNotFoundException($"Id not found: {id}");

    }
}