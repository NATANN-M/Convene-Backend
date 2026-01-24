using Convene.Infrastructure.Persistence;
using Convene.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Convene.Infrastructure.Seeders
{
    public static class EventCategorySeeder
    {
        public static async Task SeedAsync(ConveneDbContext context)
        {
            // Get existing category names for comparison
            var existingCategories = await context.EventCategories
                .Where(ec => ec.IsDefault)
                .Select(ec => ec.Name.ToLower())
                .ToListAsync();

            var defaultCategories = new[]
            {
                // Entertainment & Arts
                new { Name = "Music", Description = "Concerts, live music performances, and music festivals" },
                new { Name = "Theater & Plays", Description = "Stage performances, dramas, and theatrical shows" },
                new { Name = "Comedy", Description = "Stand-up comedy shows, improv, and comedy festivals" },
                new { Name = "Film & Cinema", Description = "Film screenings, premieres, and film festivals" },
                new { Name = "Art Exhibition", Description = "Art galleries, exhibitions, and art shows" },
                
                // Business & Professional
                new { Name = "Conference", Description = "Business conferences, industry summits, and professional gatherings" },
                new { Name = "Workshop", Description = "Training sessions, hands-on workshops, and skill-building events" },
                new { Name = "Seminar", Description = "Educational seminars, lectures, and knowledge-sharing sessions" },
                new { Name = "Networking", Description = "Business networking events, meetups, and professional mixers" },
                new { Name = "Trade Show", Description = "Industry trade shows, expos, and exhibitions" },
                
                // Sports & Fitness
                new { Name = "Sports", Description = "Sporting events, competitions, and matches" },
                new { Name = "Marathon & Racing", Description = "Running events, marathons, and races" },
                new { Name = "Fitness Class", Description = "Group fitness classes, yoga sessions, and workout events" },
                new { Name = "Outdoor Adventure", Description = "Hiking, camping, and outdoor adventure activities" },
                
                // Food & Drink
                new { Name = "Food Festival", Description = "Food festivals, culinary events, and tasting sessions" },
                new { Name = "Wine Tasting", Description = "Wine tasting events, vineyard tours, and wine festivals" },
                new { Name = "Cooking Class", Description = "Cooking workshops, culinary classes, and baking sessions" },
                
                // Technology
                new { Name = "Tech Meetup", Description = "Technology meetups, hackathons, and dev gatherings" },
                new { Name = "Webinar", Description = "Online seminars, virtual workshops, and web conferences" },
                new { Name = "Startup Pitch", Description = "Startup pitch events, demo days, and investor meetings" },
                
                // Education & Learning
                new { Name = "University Lecture", Description = "Academic lectures, university events, and educational talks" },
                new { Name = "Book Reading", Description = "Book readings, author signings, and literary events" },
                new { Name = "Language Exchange", Description = "Language learning events, conversation exchanges, and cultural events" },
                
                // Community & Social
                new { Name = "Festival", Description = "Community festivals, cultural fairs, and seasonal celebrations" },
                new { Name = "Charity & Fundraiser", Description = "Charity events, fundraising galas, and benefit dinners" },
                new { Name = "Volunteering", Description = "Volunteer opportunities, community service events, and social impact activities" },
                new { Name = "Religious & Spiritual", Description = "Religious services, spiritual gatherings, and meditation events" },
                
                // Family & Kids
                new { Name = "Kids Activities", Description = "Children's events, family fun days, and kid-friendly activities" },
                new { Name = "Educational Kids", Description = "Educational events for children, STEM activities, and learning workshops" },
                
                // Health & Wellness
                new { Name = "Wellness Retreat", Description = "Wellness retreats, meditation sessions, and mindfulness workshops" },
                new { Name = "Health Seminar", Description = "Health education, medical talks, and wellness seminars" },
                
                // Nightlife
                new { Name = "Club Night", Description = "Nightclub events, DJ nights, and party events" },
                new { Name = "Karaoke", Description = "Karaoke nights, singing events, and musical gatherings" },
                
                // Hobbies & Special Interests
                new { Name = "Gaming Tournament", Description = "Video game tournaments, esports events, and gaming competitions" },
                new { Name = "Board Games", Description = "Board game nights, tabletop gaming events, and card game tournaments" },
                new { Name = "Photography", Description = "Photography walks, workshops, and photo exhibitions" },
                new { Name = "Gardening", Description = "Gardening workshops, plant swaps, and horticultural events" }
            };

            // Only add categories that don't exist
            var categoriesToAdd = defaultCategories
                .Where(dc => !existingCategories.Contains(dc.Name.ToLower()))
                .Select(dc => new EventCategory
                {
                    Name = dc.Name,
                    Description = dc.Description,
                    IsDefault = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList();

            if (categoriesToAdd.Any())
            {
                await context.EventCategories.AddRangeAsync(categoriesToAdd);
                await context.SaveChangesAsync();
                Console.WriteLine($" Added {categoriesToAdd.Count} missing event categories: {string.Join(", ", categoriesToAdd.Select(c => c.Name))}");
            }
            else
            {
                Console.WriteLine(" All default event categories already exist. Skipping.");
            }
        }
    }
}
