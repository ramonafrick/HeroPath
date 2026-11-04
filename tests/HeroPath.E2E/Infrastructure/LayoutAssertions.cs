namespace HeroPath.E2E.Infrastructure;

/// <summary>Geometry checks that a screenshot alone cannot guarantee.</summary>
public static class LayoutAssertions
{
    /// <summary>
    /// Every repeat header («Wiederhole [-] N [+] mal») must sit on one line: select button, minus, count, plus and trailing text
    /// share one vertical centre, do not overlap each other and stay inside the block row.
    /// </summary>
    public static async Task AssertRepeatHeadersOnOneLineAsync(IPage page)
    {
        var problems = await page.EvaluateAsync<string[]>(@"() => {
            const find = id => document.querySelector(`[data-testid='${id}']`);
            const problems = [];
            for (const block of document.querySelectorAll('[data-testid=program-block][data-kind=repeat]')) {
                const path = block.dataset.path;
                const parts = ['program-select-', 'repeat-minus-', 'repeat-count-', 'repeat-plus-', 'repeat-after-']
                    .map(prefix => ({ id: prefix + path, rect: find(prefix + path).getBoundingClientRect() }));
                const row = find('block-row-' + path).getBoundingClientRect();
                const centres = parts.map(p => p.rect.top + p.rect.height / 2);
                if (Math.max(...centres) - Math.min(...centres) > 3) {
                    problems.push(`repeat ${path}: header wraps (centres ${centres.map(c => Math.round(c)).join(',')})`);
                }
                parts.forEach((p, i) => {
                    if (p.rect.left < row.left - 0.5 || p.rect.right > row.right + 0.5) {
                        problems.push(`repeat ${path}: ${p.id} leaves the row`);
                    }
                    if (i > 0 && p.rect.left < parts[i - 1].rect.right - 0.5) {
                        problems.push(`repeat ${path}: ${p.id} overlaps ${parts[i - 1].id}`);
                    }
                });
            }
            return problems;
        }");
        Assert.Empty(problems);
    }


    /// <summary>
    /// Nothing in the editor's columns needs an internal scroll (scrollHeight not above clientHeight) and the template select lies fully
    /// inside its column: for states that must fit without scrolling.
    /// </summary>
    public static async Task AssertEditorColumnsFitAsync(IPage page)
    {
        var problems = await page.EvaluateAsync<string[]>(@"() => {
            const problems = [];
            for (const column of document.querySelectorAll('[data-testid=editor-panel] > *')) {
                if (column.scrollHeight > column.clientHeight) {
                    problems.push(`column overflows: scrollHeight ${column.scrollHeight} > clientHeight ${column.clientHeight}`);
                }
                if (column.scrollWidth > column.clientWidth) {
                    problems.push(`column scrolls horizontally: ${column.scrollWidth} > ${column.clientWidth}`);
                }
            }
            const select = document.querySelector('[data-testid=editor-template]');
            const r = select.getBoundingClientRect();
            const panel = document.querySelector('[data-testid=editor-panel]').getBoundingClientRect();
            if (r.top < panel.top || r.bottom > panel.bottom || r.bottom > window.innerHeight) {
                problems.push(`template select ${Math.round(r.top)}-${Math.round(r.bottom)} not fully visible`);
            }
            return problems;
        }");
        Assert.Empty(problems);
    }

    /// <summary>No tool label overflows its button or breaks inside a word (each one-word label stays on one line).</summary>
    public static async Task AssertToolLabelsFitAsync(IPage page)
    {
        var problems = await page.EvaluateAsync<string[]>(@"() => {
            const problems = [];
            for (const tool of document.querySelectorAll('[data-testid^=editor-tool-]')) {
                const name = tool.querySelector('.tool-name');
                if (name.scrollWidth > name.clientWidth + 1) {
                    problems.push(`${name.textContent}: overflows (${name.scrollWidth} > ${name.clientWidth})`);
                }
                const range = document.createRange();
                range.selectNodeContents(name);
                if (!name.textContent.includes(' ') && range.getClientRects().length > 1) {
                    problems.push(`${name.textContent}: broken inside the word`);
                }
            }
            return problems;
        }");
        Assert.Empty(problems);
    }

    /// <summary>The page itself must not scroll: board and panels fit the viewport.</summary>
    public static async Task AssertNoPageScrollAsync(IPage page)
    {
        var problem = await page.EvaluateAsync<string?>(@"() => {
            const root = document.documentElement;
            if (root.scrollHeight <= window.innerHeight && root.scrollWidth <= window.innerWidth) {
                return null;
            }
            const height = selector => document.querySelector(selector)?.getBoundingClientRect().height ?? 'none';
            return `page ${root.scrollWidth}x${root.scrollHeight}, viewport ${window.innerWidth}x${window.innerHeight}, `
                + `board head ${height('.board-head')}, header ${height('.app-header')}, `
                + `font ${getComputedStyle(document.body).fontFamily.split(',')[0]} ${document.fonts.size}`;
        }");
        Assert.True(problem is null, $"The play page must fit the viewport without page scrolling: {problem}");
    }

    /// <summary>Program rows keep at least 44 px height and do not touch each other, also while the list scrolls.</summary>
    public static async Task AssertRowsKeepSizeAsync(IPage page)
    {
        var problems = await page.EvaluateAsync<string[]>(@"() => {
            const rows = [...document.querySelectorAll('[data-testid^=block-row-]')].map(r => ({ id: r.dataset.testid, rect: r.getBoundingClientRect() }));
            const problems = [];
            rows.forEach((row, i) => {
                if (row.rect.height < 43.5) {
                    problems.push(`${row.id}: height ${row.rect.height}`);
                }
                if (i > 0 && row.rect.top < rows[i - 1].rect.bottom + 2) {
                    problems.push(`${row.id}: touches ${rows[i - 1].id}`);
                }
            });
            return problems;
        }");
        Assert.Empty(problems);
    }

    /// <summary>
    /// For every selector the last matching element must be fully visible inside the editor side panel (which scrolls as a whole) and inside the viewport: the result
    /// message of a save or import and the entry it created must not sit behind the panel edge.
    /// </summary>
    public static async Task AssertVisibleInEditorPanelAsync(IPage page, params string[] selectors)
    {
        var problems = await page.EvaluateAsync<string[]>(@"selectors => {
            const panel = document.querySelector('[data-testid=editor-panel]').getBoundingClientRect();
            const problems = [];
            for (const selector of selectors) {
                const matches = document.querySelectorAll(selector);
                const element = matches[matches.length - 1];
                if (!element) {
                    problems.push(`${selector}: missing`);
                    continue;
                }
                const r = element.getBoundingClientRect();
                if (r.top < panel.top - 0.5 || r.bottom > panel.bottom + 0.5) {
                    problems.push(`${selector}: ${Math.round(r.top)}-${Math.round(r.bottom)} outside panel ${Math.round(panel.top)}-${Math.round(panel.bottom)}`);
                }
                let column = element;
                while (column.parentElement && column.parentElement.dataset.testid !== 'editor-panel') {
                    column = column.parentElement;
                }
                const c = column.getBoundingClientRect();
                const margin = 8;
                if (r.top < c.top + margin || r.bottom > c.bottom - margin) {
                    problems.push(`${selector}: block ${Math.round(r.top)}-${Math.round(r.bottom)} is not whole inside its scrolling column ${Math.round(c.top)}-${Math.round(c.bottom)}`);
                }
                if (r.top < 0 || r.bottom > window.innerHeight) {
                    problems.push(`${selector}: outside the viewport`);
                }
            }
            return problems;
        }", selectors);
        Assert.Empty(problems);
    }

    /// <summary>The element is fully inside the viewport (used after a fragment navigation scrolled to it).</summary>
    public static async Task AssertInViewportAsync(IPage page, string selector)
    {
        var box = await page.Locator(selector).BoundingBoxAsync();
        Assert.NotNull(box);
        var size = page.ViewportSize!;
        Assert.True(
            box.Y >= -0.5 && box.Y + box.Height <= size.Height + 0.5 && box.X >= -0.5 && box.X + box.Width <= size.Width + 0.5,
            $"{selector} is at {box.X},{box.Y} size {box.Width}x{box.Height}, outside the {size.Width}x{size.Height} viewport.");
    }
}
