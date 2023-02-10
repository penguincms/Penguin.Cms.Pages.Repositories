using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;

namespace Penguin.Cms.Pages.Repositories
{
    public class PageRepository : AuditableEntityRepository<Page>
    {
        private const string EMPTY_URL_MESSAGE = "Url can not be null or whitespace";

        private static ConcurrentDictionary<string, Page> _cachedPages;

        private ConcurrentDictionary<string, Page> CachedPages
        {
            get
            {
                _cachedPages ??= GenerateCache();

                return _cachedPages;
            }
        }

        public PageRepository(IPersistenceContext<Page> dbContext, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
        }

        public override void AcceptMessage(Updating<Page> updateMessage)
        {
            if (updateMessage is null)
            {
                throw new System.ArgumentNullException(nameof(updateMessage));
            }

            Page entity = updateMessage.Target;

            string url = entity.Url?.ToLower(CultureInfo.CurrentCulture);

            if (url != null)
            {
                _ = CachedPages.TryRemove(url, out _);

                _ = CachedPages.TryAdd(url, entity);
            }

            entity.Parameters = entity.Parameters.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();

            base.AcceptMessage(updateMessage);
        }

        public Page GetByUrl(string url)
        {
            return this.Where(p => p.Url == url).FirstOrDefault();
        }

        public string GetContentFromCache(string url)
        {
            return string.IsNullOrWhiteSpace(url)
                ? throw new System.ArgumentException(EMPTY_URL_MESSAGE, nameof(url))
                : CachedPages[url.ToLower(CultureInfo.CurrentCulture)].Content;
        }

        public bool TryGetPageFromCache(string url, out Page page)
        {
            return string.IsNullOrWhiteSpace(url)
                ? throw new System.ArgumentException(EMPTY_URL_MESSAGE, nameof(url))
                : CachedPages.TryGetValue(url.ToLower(CultureInfo.CurrentCulture), out page);
        }

        private ConcurrentDictionary<string, Page> GenerateCache()
        {
            ConcurrentDictionary<string, Page> cache = new();

            foreach (Page p in All)
            {
                if (p.Url != null)
                {
                    _ = cache.TryAdd(p.Url.ToLower(CultureInfo.CurrentCulture), p);
                }
            }

            return cache;
        }

        public Page GetByUrl(System.Uri url)
        {
            throw new System.NotImplementedException();
        }

        public string GetContentFromCache(System.Uri url)
        {
            throw new System.NotImplementedException();
        }

        public bool TryGetPageFromCache(System.Uri url, out Page page)
        {
            throw new System.NotImplementedException();
        }
    }
}