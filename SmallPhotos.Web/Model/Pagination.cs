using System;
using System.Collections.Generic;
using System.Linq;

namespace SmallPhotos.Web.Model
{
    public class Pagination
    {
        public const int PageSize = 4;

        public static readonly Pagination Empty = new Pagination(0, 0);

        public static (IEnumerable<T> Items, int Page, int PageCount) Paginate<T>(IEnumerable<T> items, int page, Func<T, bool>? selectedItem = null)
        {
            var itemCount = items.Count();
            if (itemCount <= PageSize || page < 1)
                return (items, 1, 1);
            
            if (selectedItem != null)
            {
                var pageForSelectedItem = items.Chunk(PageSize).Select((pgItems, pgIdx) => pgItems.Any(i => selectedItem(i)) ? pgIdx + 1 : 0).FirstOrDefault(pgNum => pgNum > 0);
                if (pageForSelectedItem > 0)
                    page = pageForSelectedItem;
            }

            var pageCount = (itemCount / PageSize) + (itemCount % PageSize == 0 ? 0 : 1);
            var skip = Math.Min(page, pageCount);
            return (items.Skip((skip - 1) * PageSize).Take(PageSize), skip, pageCount);
        }

        public Pagination(int pageNumber, int pageCount)
        {
            PageNumber = pageNumber;
            PageCount = pageCount;

            if (PageCount <= 10)
            {
                Pages = Enumerable.Range(1, PageCount).Select(p => new Page(p, pageNumber == p));
            }
            else
            {
                var pages = new List<int>();
                pages.AddRange(Enumerable.Range(pageNumber - 2, 6));
                Normalise(pages);

                if (pageNumber - 4 <= 1)
                {
                    pages.InsertRange(0, Enumerable.Range(pages[0] - 2, 2));
                    Normalise(pages);
                    pages.Add(PageCount);
                }
                else if (pages[pages.Count - 1] + 2 >= PageCount)
                {
                    pages.AddRange(Enumerable.Range(pages[pages.Count - 1] + 1, 2));
                    Normalise(pages);
                    pages.Insert(0, 1);
                }
                else
                {
                    pages.Insert(0, 1);
                    pages.Add(PageCount);
                }

                Pages = pages.Select((p, idx) => new Page(p, pageNumber == p, idx + 1 < pages.Count && pages[idx + 1] != p + 1));

                void Normalise(List<int> pgs)
                {
                    var adjust = pgs[0] < 1
                        ? ((pgs[0] * -1) + 1)
                        : (pgs[pgs.Count - 1] > PageCount
                            ? (PageCount - pgs[pgs.Count - 1])
                            : 0);

                    if (adjust != 0)
                        for (var i = 0; i < pgs.Count; i++)
                            pgs[i] += adjust;
                }
            }
        }

        public int PageNumber { get; }
        public int PageCount { get; }
        public IEnumerable<Page> Pages { get; }
    }
}