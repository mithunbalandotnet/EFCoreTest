using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.Extensions.ExpressionMapping;
using EFCoreTest.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EFCoreTest.Services;

public class CodingTestService(AppDbContext db, ILogger<CodingTestService> logger, IMapper mapper) : ICodingTestService
{
    private readonly AppDbContext _db = db;
    private readonly ILogger<CodingTestService> _logger = logger;
    private readonly IMapper _mapper = mapper;

    public async Task GeneratePostSummaryReportAsync(int maxItems)
    {
        // Task placeholder:
        // - Emit REPORT_START, then up to `maxItems` lines prefixed with "POST_SUMMARY|" and
        //   finally REPORT_END. Each summary line must include PostId|AuthorName|CommentCount|LatestCommentAuthor.
        // - Method must be read-only and efficient for large datasets;
        // Implement the method body in the assessment; do not change the signature.
        _logger.LogInformation("REPORT_START");
        var query = _db.Posts
            .AsNoTracking()
            .Select(p => new {
                PostId = p.Id, Author = p.Author,
                CommentCount = p.Comments.Count(),
                LatestComment = p.Comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault(),
                LatestCommentAuthor = p.Comments.Any()? p.Comments.OrderByDescending(c => c.CreatedAt).FirstOrDefault().Author: null,
            })
            .OrderBy(p => p.PostId)
            .Take(maxItems);
        var postSummaries = await query.ToListAsync();
        foreach (var post in query)
        {
            var latestCommentAuthor = post.LatestCommentAuthor;
            var latestCommentAuthorName = latestCommentAuthor?.Name ?? "N/A";
            _logger.LogInformation("POST_SUMMARY|{PostId}|{AuthorName}|{CommentCount}|{LatestCommentAuthor}",
                post.PostId,
                post.Author?.Name ?? "N/A",
                post.CommentCount,
                latestCommentAuthorName);
        }
        _logger.LogInformation("REPORT_END");
    }

    public async Task<IList<PostDto>> SearchPostSummariesAsync(string query, int maxResults = 50)
    {
        // Task placeholder:
        // - Return at most `maxResults` PostDto entries.
        // - Treat null/empty/whitespace query as no filter (return unfiltered results up to maxResults).
        // - Matching: case-insensitive substring in Title OR Content.
        // - Order by CreatedAt descending, project to PostDto, and avoid materializing full entities.
        // Implement the method body in the assessment; do not change the signature.
        var dbquery = _db.Posts.Include(p => p.Author).Include(p => p.Comments).AsNoTracking();
        if(!string.IsNullOrEmpty(query))
        {
            dbquery = dbquery.Where(p => EF.Functions.Like(p.Title, $"%{query}%") || EF.Functions.Like(p.Content, $"%{query}%"));
        }
        dbquery = dbquery.OrderByDescending(p => p.CreatedAt).Take(maxResults);
        var result = await dbquery.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Excerpt = p.Content.Length > 100 ? p.Content.Substring(0, 100) + "..." : p.Content,
            AuthorName = p.Author != null ? p.Author.Name : null,
            CommentCount = p.Comments.Count(),
            CreatedAt = p.CreatedAt
        }).ToListAsync();
        return result;
    }

    public async Task<IList<PostDto>> SearchPostSummariesAsync<TKey>(string query, int skip, int take, Expression<Func<PostDto, TKey>> orderBySelector, bool descending)
    {
        // Task placeholder:
        // - Server-side filter by `query` (null/empty => no filter), server-side ordering based on
        //   the provided DTO selector, then Skip/Take for paging. Project to PostDto and avoid
        //   per-row queries or client-side paging.
        // - Implementations may choose which selectors to support; unsupported selectors may
        //   be rejected by the grader.
        // Implement the method body in the assessment; do not change the signature.

        Expression<Func<Post, TKey>> orderByMapped = _mapper.MapExpression<Expression<Func<Post, TKey>>>(orderBySelector);
        var dbquery = _db.Posts.Include(p => p.Author).Include(p => p.Comments).AsNoTracking();
        if (!string.IsNullOrEmpty(query))
        {
            dbquery = dbquery.Where(p => EF.Functions.Like(p.Title, $"%{query}%") || EF.Functions.Like(p.Content, $"%{query}%"));
        }
        if(descending)
        {
            dbquery = dbquery.OrderByDescending(orderByMapped);
        }
        else
        {
            dbquery = dbquery.OrderBy(orderByMapped);
        }
        dbquery = dbquery.Skip(skip).Take(take);
        var result = await dbquery.Select(p => new PostDto
        {
            Id = p.Id,
            Title = p.Title,
            Excerpt = p.Content.Length > 100 ? p.Content.Substring(0, 100) + "..." : p.Content,
            AuthorName = p.Author != null ? p.Author.Name : null,
            CommentCount = p.Comments.Count(),
            CreatedAt = p.CreatedAt
        }).ToListAsync();
        return result;
    }
}
