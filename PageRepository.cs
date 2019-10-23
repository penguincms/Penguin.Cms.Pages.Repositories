using Penguin.Cms.Pages;
using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Messaging.Persistence.Messages;
using Penguin.Persistence.Abstractions.Interfaces;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;

namespace Penguin.Cms.Pages.Repositories
{
    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    [SuppressMessage("Design", "CA1054:Uri parameters should not be strings")]
    public class PageRepository : AuditableEntityRepository<Page>
    {
        private ConcurrentDictionary<string, Page> CachedPages
        {
            get
            {
                _cachedPages = _cachedPages ?? GenerateCache();

                return _cachedPages;
            }
        }

        private const string EMPTY_URL_MESSAGE = "Url can not be null or whitespace";

        private static ConcurrentDictionary<string, Page> _cachedPages;

        public PageRepository(IPersistenceContext<Page> dbContext, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
        }

        public override void AcceptMessage(Updating<Page> update)
        {
            if (update is null)
            {
                throw new System.ArgumentNullException(nameof(update));
            }

            Page entity = update.Target;

            string url = entity.Url?.ToLower(CultureInfo.CurrentCulture);

            if (url != null)
            {
                CachedPages.TryRemove(url, out Page _);

                CachedPages.TryAdd(url, entity);
            }

            entity.Parameters = entity.Parameters.Where(p => !string.IsNullOrWhiteSpace(p.Name)).ToList();

            base.AcceptMessage(update);
        }

        public Page GetByUrl(string url) => this.Where(p => p.Url == url).FirstOrDefault();

        public string GetContentFromCache(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new System.ArgumentException(EMPTY_URL_MESSAGE, nameof(url));
            }

            return CachedPages[url.ToLower(CultureInfo.CurrentCulture)].Content;
        }

        public bool TryGetPageFromCache(string url, out Page page)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new System.ArgumentException(EMPTY_URL_MESSAGE, nameof(url));
            }

            return CachedPages.TryGetValue(url.ToLower(CultureInfo.CurrentCulture), out page);
        }

        private ConcurrentDictionary<string, Page> GenerateCache()
        {
            ConcurrentDictionary<string, Page> cache = new ConcurrentDictionary<string, Page>();

            foreach (Page p in this.All)
            {
                if (p.Url != null)
                {
                    cache.TryAdd(p.Url.ToLower(CultureInfo.CurrentCulture), p);
                }
            }

            return cache;
        }
    }
}