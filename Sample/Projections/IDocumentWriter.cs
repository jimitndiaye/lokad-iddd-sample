using System;

namespace Sample.Projections
{
    public interface IDocumentWriter<in TKey, TEntity>
    {
        TEntity AddOrUpdate(TKey key, Func<TEntity> addFactory, Func<TEntity, TEntity> update, AddOrUpdateHint hint = AddOrUpdateHint.ProbablyExists);
        bool TryDelete(TKey key);
    }
    public enum AddOrUpdateHint
    {
        ProbablyExists,
        ProbablyDoesNotExist
    }

    public static class ExtendDocumentWriter
    {
        /// <summary>
        /// Given a <paramref name="key"/> either adds a new <typeparamref name="TEntity"/> OR updates an existing one.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="addFactory">The add factory (used to create a new entity, if it is not found).</param>
        /// <param name="update">The update method (called to update an existing entity, if it exists).</param>
        /// <param name="hint">The hint.</param>
        /// <returns></returns>
        public static TEntity AddOrUpdate<TKey, TEntity>(this IDocumentWriter<TKey, TEntity> self, TKey key, Func<TEntity> addFactory, Action<TEntity> update, AddOrUpdateHint hint = AddOrUpdateHint.ProbablyExists)
        {
            return self.AddOrUpdate(key, addFactory, entity =>
            {
                update(entity);
                return entity;
            }, hint);
        }
        /// <summary>
        /// Given a <paramref name="key"/> either adds a new <typeparamref name="TEntity"/> OR updates an existing one.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="newView">The new view that will be saved, if entity does not already exist</param>
        /// <param name="updateViewFactory">The update method (called to update an existing entity, if it exists).</param>
        /// <param name="hint">The hint.</param>
        /// <returns></returns>
        public static TEntity AddOrUpdate<TKey, TEntity>(this IDocumentWriter<TKey, TEntity> self, TKey key, TEntity newView, Action<TEntity> updateViewFactory, AddOrUpdateHint hint = AddOrUpdateHint.ProbablyExists)
        {
            return self.AddOrUpdate(key, () => newView, view =>
            {
                updateViewFactory(view);
                return view;
            }, hint);
        }

        /// <summary>
        /// Saves new entity, using the provided <param name="key"></param> and throws 
        /// <exception cref="InvalidOperationException"></exception> if the entity actually already exists
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="newEntity">The new entity.</param>
        /// <returns></returns>
        public static TEntity Add<TKey, TEntity>(this IDocumentWriter<TKey, TEntity> self, TKey key, TEntity newEntity)
        {
            return self.AddOrUpdate(key, newEntity, e =>
            {
                var txt = String.Format("Entity '{0}' with key '{1}' should not exist.", typeof(TEntity).Name, key);
                throw new InvalidOperationException(txt);
            }, AddOrUpdateHint.ProbablyDoesNotExist);
        }


        /// <summary>
        /// Updates already existing entity, throwing exception, if it does not already exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="change">The change.</param>
        /// <returns></returns>
        public static TEntity UpdateOrThrow<TKey, TEntity>(this IDocumentWriter<TKey, TEntity> self, TKey key, Func<TEntity, TEntity> change)
        {
            return self.AddOrUpdate(key, () =>
            {
                var txt = String.Format("Failed to load '{0}' with key '{1}'.", typeof(TEntity).Name, key);
                throw new InvalidOperationException(txt);
            }, change, AddOrUpdateHint.ProbablyExists);
        }
        /// <summary>
        /// Updates already existing entity, throwing exception, if it does not already exist.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TEntity">The type of the entity.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="change">The change.</param>
        /// <returns></returns>
        public static TEntity UpdateOrThrow<TKey, TEntity>(this IDocumentWriter<TKey, TEntity> self, TKey key, Action<TEntity> change)
        {
            return self.AddOrUpdate(key, () =>
            {
                var txt = String.Format("Failed to load '{0}' with key '{1}'.", typeof(TEntity).Name, key);
                throw new InvalidOperationException(txt);
            }, change, AddOrUpdateHint.ProbablyExists);
        }

        /// <summary>
        /// Updates an entity, creating a new instance before that, if needed.
        /// </summary>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TView">The type of the view.</typeparam>
        /// <param name="self">The self.</param>
        /// <param name="key">The key.</param>
        /// <param name="update">The update.</param>
        /// <param name="hint">The hint.</param>
        /// <returns></returns>
        public static TView UpdateEnforcingNew<TKey, TView>(this IDocumentWriter<TKey, TView> self, TKey key,
            Action<TView> update, AddOrUpdateHint hint = AddOrUpdateHint.ProbablyExists)
            where TView : new()
        {
            return self.AddOrUpdate(key, () =>
            {
                var view = new TView();
                update(view);
                return view;
            }, v =>
            {
                update(v);
                return v;
            }, hint);
        }
    }
}