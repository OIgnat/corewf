// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Expressions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Reflection;
    using CoreWf.Internals;
    using Portable.Xaml;

    public static class TextExpression
    {
        private static readonly AttachableMemberIdentifier namespacesProperty = 
            new AttachableMemberIdentifier(typeof(TextExpression), "Namespaces");

        private static readonly AttachableMemberIdentifier namespacesForImplementationProperty =
            new AttachableMemberIdentifier(typeof(TextExpression), "NamespacesForImplementation");

        private static readonly AttachableMemberIdentifier referencesProperty =
            new AttachableMemberIdentifier(typeof(TextExpression), "References");

        private static readonly AttachableMemberIdentifier referencesForImplementationProperty =
            new AttachableMemberIdentifier(typeof(TextExpression), "ReferencesForImplementation");

        // This should be kept consistent with VisualBasicSettings.defaultImportReferences
        private static readonly ReadOnlyCollection<string> defaultNamespaces = new ReadOnlyCollection<string>(new string[]
        {
            "System",
            "System.Collections",
            "System.Collections.Generic",
            "CoreWf",
            "CoreWf.Expressions",
            "CoreWf.Statements"
        });

        private static readonly ReadOnlyCollection<AssemblyReference> defaultReferences = new ReadOnlyCollection<AssemblyReference>(new AssemblyReference[]
        {
            new AssemblyName("mscorlib"),
            new AssemblyName("System"),
            new AssemblyName("CoreWf"),
            new AssemblyName("System.Core")
        });

        public static IList<string> DefaultNamespaces
        {
            get
            {
                return TextExpression.defaultNamespaces;
            }
        }

        public static IList<AssemblyReference> DefaultReferences
        {
            get
            {
                return TextExpression.defaultReferences;
            }
        }

        public static IList<string> GetNamespacesInScope(Activity activity)
        {
            Activity root = GetRoot(activity, out bool isImplementation);

            IList<string> result = isImplementation ? GetNamespacesForImplementation(root) : GetNamespaces(root);
            if (result.Count == 0 && !isImplementation && GetReferences(root).Count == 0)
            {
                // If this is a public child, but there are no public setings, this activity was
                // probably a default set as part of the activity definition; so fall back to the
                // implementation settings.
                result = GetNamespacesForImplementation(root);
            }

            return result;
        }

        public static IList<string> GetNamespaces(object target)
        {
            return GetCollection<string>(target, namespacesProperty);
        }

        public static void SetNamespaces(object target, IList<string> namespaces)
        {
            SetCollection(target, namespacesProperty, namespaces);
        }

        public static void SetNamespaces(object target, params string[] namespaces)
        {
            SetCollection(target, namespacesProperty, namespaces);
        }

        public static bool ShouldSerializeNamespaces(object target)
        {
            return ShouldSerializeCollection<string>(target, namespacesProperty);
        }

        public static IList<string> GetNamespacesForImplementation(object target)
        {
            return GetCollection<string>(target, namespacesForImplementationProperty);
        }

        public static void SetNamespacesForImplementation(object target, IList<string> namespaces)
        {
            SetCollection(target, namespacesForImplementationProperty, namespaces);
        }

        public static void SetNamespacesForImplementation(object target, params string[] namespaces)
        {
            SetCollection(target, namespacesForImplementationProperty, namespaces);
        }

        // Implementation namespaces only serialize when the activity is being defined (target is ActivityBuilder),
        // not when it is being consumed (target is Activity)
        public static bool ShouldSerializeNamespacesForImplementation(object target)
        {
            return !(target is Activity) && ShouldSerializeCollection<string>(target, namespacesForImplementationProperty);
        }

        public static IList<AssemblyReference> GetReferencesInScope(Activity activity)
        {
            Activity root = GetRoot(activity, out bool isImplementation);

            IList<AssemblyReference> result = isImplementation ? GetReferencesForImplementation(root) : GetReferences(root);
            if (result.Count == 0 && !isImplementation && GetNamespaces(root).Count == 0)
            {
                // If this is a public child, but there are no public setings, this activity was
                // probably a default set as part of the activity definition; so fall back to the
                // implementation settings.
                result = GetReferencesForImplementation(root);
            }

            return result;
        }

        public static IList<AssemblyReference> GetReferences(object target)
        {
            return GetCollection<AssemblyReference>(target, referencesProperty);
        }

        public static void SetReferences(object target, IList<AssemblyReference> references)
        {
            SetCollection(target, referencesProperty, references);
        }

        public static void SetReferences(object target, params AssemblyReference[] references)
        {
            SetCollection(target, referencesProperty, references);
        }

        public static bool ShouldSerializeReferences(object target)
        {
            return ShouldSerializeCollection<AssemblyReference>(target, referencesProperty);
        }

        public static IList<AssemblyReference> GetReferencesForImplementation(object target)
        {
            return GetCollection<AssemblyReference>(target, referencesForImplementationProperty);
        }

        public static void SetReferencesForImplementation(object target, IList<AssemblyReference> references)
        {
            SetCollection(target, referencesForImplementationProperty, references);
        }

        public static void SetReferencesForImplementation(object target, params AssemblyReference[] references)
        {
            SetCollection(target, referencesForImplementationProperty, references);
        }

        // Implementation references only serialize when the activity is being defined (target is ActivityBuilder),
        // not when it is being consumed (target is Activity)
        public static bool ShouldSerializeReferencesForImplementation(object target)
        {
            return !(target is Activity) && ShouldSerializeCollection<AssemblyReference>(target, referencesForImplementationProperty);
        }

        internal static bool LanguagesAreEqual(string left, string right)
        {
            // CodeDOM languages are case-insensitive
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }

        private static Activity GetRoot(Activity activity, out bool isImplementation)
        {
            isImplementation = false;
            Activity root = null;
            if (activity != null)
            {
                if (activity.MemberOf == null)
                {
                    throw FxTrace.Exception.Argument(nameof(activity), SR.ActivityIsUncached);
                }

                LocationReferenceEnvironment environment = activity.GetParentEnvironment();
                isImplementation = activity.MemberOf != activity.RootActivity.MemberOf;
                root = environment.Root;
            }

            return root;
        }

        private static IList<T> GetCollection<T>(object target, AttachableMemberIdentifier property)
        {
            if (!AttachablePropertyServices.TryGetProperty(target, property, out IList<T> result))
            {
                result = new Collection<T>();
                AttachablePropertyServices.SetProperty(target, property, result);
            }

            return result;
        }

        private static void SetCollection<T>(object target, AttachableMemberIdentifier property, IList<T> collection)
        {
            if (collection == null)
            {
                AttachablePropertyServices.RemoveProperty(target, property);
            }
            else
            {
                if (collection is Array)
                {
                    collection = new Collection<T>(collection);
                }

                AttachablePropertyServices.SetProperty(target, property, collection);
            }
        }

        // We need this explicit check because otherwise, an empty collection might get serialized
        // just because the getter was accessed
        private static bool ShouldSerializeCollection<T>(object target, AttachableMemberIdentifier property)
        {
            if (AttachablePropertyServices.TryGetProperty(target, property, out IList<T> result))
            {
                return result.Count > 0;
            }

            return false;
        }
    }
}
