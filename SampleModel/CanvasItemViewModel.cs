﻿using System.Threading;
using Glass.Design.Pcl.Canvas;
using PostSharp.Patterns.Contracts;

namespace SampleModel
{
    public class CanvasItemViewModel : CanvasModelItem
    {
        private static int nextId;
        private readonly int id = Interlocked.Increment(ref nextId);
        
        public CanvasItemViewModel()
        {
            this.Name = this.GetType().Name + id;
        }

        [Required]
        public string Name { get; set; }

        public override string GetName()
        {
            return this.Name;
        }


        public override string ToString()
        {
            // For better debugging.
            return string.Format("{0} #{5}, Left={1}, Top={2}, Width={3}, Height={4}", this.GetType().Name, this.Left,
                this.Top, this.Width, this.Height, this.id);
        }                
    }
}