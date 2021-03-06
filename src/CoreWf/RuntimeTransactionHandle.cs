// This file is part of Core WF which is licensed under the MIT license.
// See LICENSE file in the project root for full license information.

namespace CoreWf
{
    using System;
    using CoreWf.Runtime;
    using System.Runtime.Serialization;
    using CoreWf.Transactions;
    using CoreWf.Internals;

    [Fx.Tag.XamlVisible(false)]
    [DataContract]
    public sealed class RuntimeTransactionHandle : Handle, IExecutionProperty, IPropertyRegistrationCallback
    {
        private ActivityExecutor executor;
        private bool isHandleInitialized;
        private bool doNotAbort;
        private bool isPropertyRegistered;
        private bool isSuppressed;
        private TransactionScope scope;
        private readonly Transaction rootTransaction;

        public RuntimeTransactionHandle()
        {
        }

        // This ctor is used when we want to make a transaction ambient
        // without enlisting.  This is desirable for scenarios like WorkflowInvoker
        public RuntimeTransactionHandle(Transaction rootTransaction)
        {
            this.rootTransaction = rootTransaction ?? throw FxTrace.Exception.ArgumentNull(nameof(rootTransaction));
            this.AbortInstanceOnTransactionFailure = false;
        }

        public bool AbortInstanceOnTransactionFailure
        {
            get
            {
                return !this.doNotAbort;
            }
            set
            {
                ThrowIfRegistered(SR.CannotChangeAbortInstanceFlagAfterPropertyRegistration);
                this.doNotAbort = !value;
            }
        }

        public bool SuppressTransaction
        {
            get
            {
                return this.isSuppressed;
            }
            set 
            {
                ThrowIfRegistered(SR.CannotSuppressAlreadyRegisteredHandle);
                this.isSuppressed = value;
            }
        }

        [DataMember(Name = "executor")]
        internal ActivityExecutor SerializedExecutor
        {
            get { return this.executor; }
            set { this.executor = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "isHandleInitialized")]
        internal bool SerializedIsHandleInitialized
        {
            get { return this.isHandleInitialized; }
            set { this.isHandleInitialized = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "doNotAbort")]
        internal bool SerializedDoNotAbort
        {
            get { return this.doNotAbort; }
            set { this.doNotAbort = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "isPropertyRegistered")]
        internal bool SerializedIsPropertyRegistered
        {
            get { return this.isPropertyRegistered; }
            set { this.isPropertyRegistered = value; }
        }

        [DataMember(EmitDefaultValue = false, Name = "isSuppressed")]
        internal bool SerializedIsSuppressed
        {
            get { return this.isSuppressed; }
            set { this.isSuppressed = value; }
        }

        internal bool IsRuntimeOwnedTransaction
        {
            get { return this.rootTransaction != null; }
        }

        private void ThrowIfRegistered(string message)
        {
            if (this.isPropertyRegistered)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(message));
            }
        }

        private void ThrowIfNotRegistered(string message)
        {
            if (!this.isPropertyRegistered)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(message));
            }
        }
        
        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //    Justification = "This method is designed to be called from activities with handle access.")]
        public Transaction GetCurrentTransaction(NativeActivityContext context)
        {
            return GetCurrentTransactionCore(context);
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //    Justification = "This method is designed to be called from activities with handle access.")]
        public Transaction GetCurrentTransaction(CodeActivityContext context)
        {
            return GetCurrentTransactionCore(context);
        }

        //[SuppressMessage(FxCop.Category.Design, FxCop.Rule.ConsiderPassingBaseTypesAsParameters,
        //    Justification = "This method is designed to be called from activities with handle access.")]
        public Transaction GetCurrentTransaction(AsyncCodeActivityContext context)
        {
            return GetCurrentTransactionCore(context);
        }

        private Transaction GetCurrentTransactionCore(ActivityContext context)
        {
            if (context == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(context));
            }
            
            context.ThrowIfDisposed();

            //If the transaction is a runtime transaction (i.e. an Invoke with ambient transaction case), then 
            //we do not require that it be registered since the handle created for the root transaction is never registered.
            if (this.rootTransaction == null)
            {
                this.ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("GetCurrentTransaction"));
            }

            if (!this.isHandleInitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
            }

            if (this.SuppressTransaction)
            {
                return null;
            }

            return this.executor.CurrentTransaction;
        }

        protected override void OnInitialize(HandleInitializationContext context)
        {
            this.executor = context.Executor;
            this.isHandleInitialized = true;

            if (this.rootTransaction != null)
            {
                Fx.Assert(this.Owner == null, "this.rootTransaction should only be set at the root");
                this.executor.SetTransaction(this, this.rootTransaction, null, null);
            }

            base.OnInitialize(context);
        }

        protected override void OnUninitialize(HandleInitializationContext context)
        {
            if (this.rootTransaction != null)
            {
                // If we have a host transaction we're responsible for exiting no persist
                this.executor.ExitNoPersist();
            }

            this.isHandleInitialized = false;
            base.OnUninitialize(context);
        }

        public void RequestTransactionContext(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state)
        {
            RequestOrRequireTransactionContextCore(context, callback, state, false);
        }

        public void RequireTransactionContext(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state)
        {
            RequestOrRequireTransactionContextCore(context, callback, state, true);
        }

        private void RequestOrRequireTransactionContextCore(NativeActivityContext context, Action<NativeActivityTransactionContext, object> callback, object state, bool isRequires)
        {
            if (context == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(context));
            }

            context.ThrowIfDisposed();

            if (context.HasRuntimeTransaction)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionAlreadyExists));
            }

            if (context.IsInNoPersistScope)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotSetRuntimeTransactionInNoPersist));
            }

            if (!this.isHandleInitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
            }

            if (this.SuppressTransaction)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionIsSuppressed));
            }

            if (isRequires)
            {
                if (context.RequiresTransactionContextWaiterExists)
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(SR.OnlyOneRequireTransactionContextAllowed));
                }

                this.ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("RequireTransactionContext"));
            }
            else
            {
                this.ThrowIfNotRegistered(SR.RuntimeTransactionHandleNotRegisteredAsExecutionProperty("RequestTransactionContext"));
            }

            context.RequestTransactionContext(isRequires, this, callback, state);
        }   

        public void CompleteTransaction(NativeActivityContext context)
        {
            CompleteTransactionCore(context, null);
        }

        public void CompleteTransaction(NativeActivityContext context, BookmarkCallback callback)
        {
            if (callback == null)
            {
                throw FxTrace.Exception.ArgumentNull(nameof(callback));
            }

            CompleteTransactionCore(context, callback);
        }

        private void CompleteTransactionCore(NativeActivityContext context, BookmarkCallback callback)
        {
            context.ThrowIfDisposed();

            if (this.rootTransaction != null)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.CannotCompleteRuntimeOwnedTransaction));
            }

            if (!context.HasRuntimeTransaction)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.NoRuntimeTransactionExists));
            }

            if (!this.isHandleInitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
            }

            if (this.SuppressTransaction)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.RuntimeTransactionIsSuppressed));
            }

            context.CompleteTransaction(this, callback);
        }

        [Fx.Tag.Throws(typeof(TransactionException), "The transaction for this property is in a state incompatible with TransactionScope.")]
        void IExecutionProperty.SetupWorkflowThread()
        {
            if (this.SuppressTransaction)
            {
                this.scope = new TransactionScope(TransactionScopeOption.Suppress);
                return;
            }

            if ((this.executor != null) && this.executor.HasRuntimeTransaction)
            {
                this.scope = TransactionHelper.CreateTransactionScope(this.executor.CurrentTransaction);
            }
        }

        void IExecutionProperty.CleanupWorkflowThread()
        {
            TransactionHelper.CompleteTransactionScope(ref this.scope);
        }

        void IPropertyRegistrationCallback.Register(RegistrationContext context)
        {
            if (!this.isHandleInitialized)
            {
                throw FxTrace.Exception.AsError(new InvalidOperationException(SR.UnInitializedRuntimeTransactionHandle));
            }

            RuntimeTransactionHandle handle = (RuntimeTransactionHandle)context.FindProperty(typeof(RuntimeTransactionHandle).FullName);
            if (handle != null)
            {
                if (handle.SuppressTransaction)
                {
                    this.isSuppressed = true;
                }
            }

            this.isPropertyRegistered = true;
        }

        void IPropertyRegistrationCallback.Unregister(RegistrationContext context)
        {
            this.isPropertyRegistered = false;
        }
    }
}
