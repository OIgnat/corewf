// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf.Tracking
{
    using System;
    using System.Globalization;
    using System.Runtime.Serialization;
    using CoreWf.Runtime;
    using CoreWf.Internals;

    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class BookmarkResumptionRecord : TrackingRecord
    {
        private Guid bookmarkScope;
        private string bookmarkName;
        private object payload;
        private ActivityInfo owner;

        internal BookmarkResumptionRecord(Guid instanceId, Bookmark bookmark, ActivityInstance ownerInstance, object payload)
            : base(instanceId)
        {
            if (bookmark.Scope != null)
            {
                this.BookmarkScope = bookmark.Scope.Id;
            }

            if (bookmark.IsNamed)
            {
                this.BookmarkName = bookmark.Name;
            }

            this.Owner = new ActivityInfo(ownerInstance);
            this.Payload = payload;
        }

        public BookmarkResumptionRecord(Guid instanceId, long recordNumber, Guid bookmarkScope, string bookmarkName, ActivityInfo owner)
            : base(instanceId, recordNumber)
        {
            this.BookmarkScope = bookmarkScope;
            this.BookmarkName = bookmarkName;
            this.Owner = owner ?? throw FxTrace.Exception.ArgumentNull(nameof(owner));
        }

        private BookmarkResumptionRecord(BookmarkResumptionRecord record)
            : base(record)
        {
            this.BookmarkScope = record.BookmarkScope;
            this.Owner = record.Owner;
            this.BookmarkName = record.BookmarkName;
            this.Payload = record.Payload;           
        }
        
        public Guid BookmarkScope
        {
            get
            {
                return bookmarkScope;
            }
            private set
            {
                this.bookmarkScope = value;
            }
        }
        
        public string BookmarkName
        {
            get
            {
                return this.bookmarkName;
            }
            private set
            {
                this.bookmarkName = value;
            }
        }

        public object Payload
        {
            get { return this.payload; }
            internal set { this.payload = value; }
        }
        
        public ActivityInfo Owner
        {
            get
            {
                return this.owner;
            }
            private set
            {
                this.owner = value;
            }
        }

        [DataMember(EmitDefaultValue = false, Name = "BookmarkScope")]
        internal Guid SerializedBookmarkScope
        {
            get { return this.BookmarkScope; }
            set { this.BookmarkScope = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "BookmarkName")]
        internal string SerializedBookmarkName
        {
            get { return this.BookmarkName; }
            set { this.BookmarkName = value; }
        }

        [DataMember(Name = "Payload")]
        internal object SerializedPayload
        {
            get { return this.Payload; }
            set { this.Payload = value; }
        }

        [DataMember(Name = "Owner")]
        internal ActivityInfo SerializedOwner
        {
            get { return this.Owner; }
            set { this.Owner = value; }
        }

        protected internal override TrackingRecord Clone()
        {
            return new BookmarkResumptionRecord(this);
        }

        public override string ToString()
        {
            return string.Format(CultureInfo.CurrentCulture,
                "BookmarkResumptionRecord {{ {0}, BookmarkName = {1}, BookmarkScope = {2}, OwnerActivity {{ {3} }} }}",
                base.ToString(),
                this.BookmarkName ?? "<null>",
                this.BookmarkScope,
                this.Owner.ToString());
        }
    }
}
