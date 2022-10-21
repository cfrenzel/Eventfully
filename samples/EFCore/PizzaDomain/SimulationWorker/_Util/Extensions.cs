using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace SimulationWorker
{
    public static class Extensions
    {
        const string CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        public static string String(this Random r, int length)
        {
            return new string(Enumerable.Repeat(CHARS, length)
                .Select(s => s[r.Next(s.Length)]).ToArray());
        }
        
        public static Decimal Money(this Random r, int maxDollars = 99, int maxCents = 99)
        {
            var dollar = r.Next(9, maxDollars + 1);
            var cents = r.Next(0, maxCents + 1) / 100;
            return dollar + cents;
        }
        public static T Choose<T>(this Random r, params T[] choices)
        {
            int index = r.Next(choices.Length);
            return choices[index];
        }
        
       
    }
}