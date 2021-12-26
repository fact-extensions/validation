using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fact.Extensions.Validation.Experimental
{
    public class InputProcessor
    {
        //readonly Processor<Context2> processor = new Processor<Context2>();

        LinkedList<Item> items = new LinkedList<Item>();

        public class Item
        {
            public Item Previous { get; }
            
            /// <summary>
            /// Set to true when a party either external or internal wants to indicate
            /// that value has changed that this processor can retrieve.
            /// Only 'Process' method should set this to false
            /// </summary>
            public bool HasChanged { get; set; }
            
            /// <summary>
            /// Retrieves a value from an input source, and possibly merges it in with
            /// existing
            /// </summary>
            /// <remarks>
            /// Only invoked if this or a predecessor has 'HasChanged' = true
            /// 'Item.HasChanged' indicates if this particular processor has a 'HasChanged' condition
            /// </remarks>
            public Func<Item, object, object> Process { get; }
            
            /// <summary>
            /// Cached value retrieved from the last process chain.
            /// Invalid if HasChanged = true and outside of 'Process' chain
            /// </summary>
            public object LastValue { get; internal set; }

            public Item(Item previous, Func<Item, object, object> valueProcessor)
            {
                Previous = previous;
                Process = valueProcessor;
            }
        }
        
        
        public Item Add(Func<Item, object, object> valueProcessor)
        {
            Item item = new Item(items.Last?.Value, valueProcessor);
            items.AddLast(item);
            /*
            processor.ProcessingAsync += (_, context) =>
            {
                if (item.HasChanged)
                {
                    
                }
                return new ValueTask();
            }; */
            return item;
        }

        // value probably can come in from IBinderBase, but we may phase that out completely
        // in which case value will come from the first in the item list
        public async Task<object> Process(object value)
        {
            // DEBT: Likely there's a way to optimize which item 'starts' the change chain

            bool hasChanged = false;
            
            foreach (Item item in items)
            {
                if (item.HasChanged)
                    hasChanged = true;
                
                if (hasChanged)
                {
                    value = item.Process(item, value);
                    item.LastValue = value;
                    item.HasChanged = false;
                }
            }

            return Task.FromResult(value);
        }
    }
}