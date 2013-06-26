using System.Collections.Generic;
using System.Linq;

namespace WebGrease.Configuration
{
    using System.Collections;

    public class ResourcePivotGroupCollection : IEnumerable<ResourcePivotGroup>
    {
        private readonly IDictionary<string, ResourcePivotGroup> resourcePivots = new Dictionary<string, ResourcePivotGroup>();

        public ResourcePivotGroup this[string groupKey]
        {
            get
            {
                ResourcePivotGroup resourcePivotGroup;
                if (this.resourcePivots.TryGetValue(groupKey, out resourcePivotGroup))
                {
                    return resourcePivotGroup;
                }

                return null;
            }
        }

        internal void Clear(string groupKey)
        {
            var resourcePivotGroup = this[groupKey];
            if (resourcePivotGroup != null)
            {
                resourcePivotGroup.Keys.Clear();
            }
        }

        internal void Set(string groupKey, ResourcePivotApplyMode? applyMode, IEnumerable<string> keys)
        {
            var resourcePivotGroup = this[groupKey];
            if (resourcePivotGroup != null)
            {
                resourcePivotGroup = new ResourcePivotGroup(groupKey, applyMode ?? resourcePivotGroup.ApplyMode, resourcePivotGroup.Keys.Concat(keys));
            }
            else
            {
                resourcePivotGroup = new ResourcePivotGroup(groupKey, applyMode ?? ResourcePivotApplyMode.ApplyAsStringReplace, keys);
            }

            this.resourcePivots[groupKey] = resourcePivotGroup;
        }

        public IEnumerator<ResourcePivotGroup> GetEnumerator()
        {
            return this.resourcePivots.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    public class ResourcePivotKey
    {
        public ResourcePivotKey(string groupKey, string key)
        {
            this.GroupKey = groupKey;
            this.Key = key;
        }

        public string GroupKey { get; private set; }
        public string Key { get; private set; }
    }

    public class ResourcePivotGroup
    {
        public ResourcePivotGroup(string key, ResourcePivotApplyMode applyMode, IEnumerable<string> keys)
        {
            this.Key = key;
            this.ApplyMode = applyMode;
            this.Keys = new HashSet<string>(keys);
        }

        public HashSet<string> Keys { get; private set; }
        public string Key { get; private set; }
        public ResourcePivotApplyMode ApplyMode { get; private set; }
    }

    public enum ResourcePivotApplyMode
    {
        ApplyAsStringReplace,
        CssApplyAfterParse,
    }
}
