namespace GVMServer.Ns.Collections
{
    using System;
    using GVMServer.Ns.Enum;
    using GVMServer.Ns.Functional;
    using GVMServer.Utilities;

    public class Monitor
    {
        private IDisposable disposable = null;

        public string Id { get; }

        public Monitor(decimal lockId) : this(lockId.ToString())
        {

        }

        public Monitor(string lockId)
        {
            if (string.IsNullOrEmpty(lockId))
            {
                throw new ArgumentOutOfRangeException(nameof(lockId));
            }
            this.Id = lockId;
        }

        public bool Locking
        {
            get
            {
                bool localTaken = this.LocalTaken(out Error error);
                if (error != Error.Error_Success)
                {
                    throw new RuntimeException(error);
                }
                return localTaken;
            }
        }

        public virtual bool LocalTaken(out Error error)
        {
            bool locked = false;
            error = CacheAccessor.GetClient((storage) => 
            {
                var e = CacheAccessor.GetValue(storage, this.GetKey(), out long ts);
                if (e == Error.Error_Success)
                {
                    locked = unchecked(0 != ts);
                    if (locked)
                    {
                        if (ts < 0)
                        {
                            locked = false;
                        }
                        else
                        {
                            DateTime matureness = ts.FromTimespan13().Add(TimeZoneInfo.Local.BaseUtcOffset);
                            locked = matureness > DateTime.Now;
                        }
                    }
                }
                return e;
            });
            return locked;
        }

        private string GetKey()
        {
            return $"ns.core.collections.sysobj.monitor.{this.Id}";
        }

        public virtual Error Enter(int timeout = 3)
        {
            lock (this)
            {
                if (this.disposable != null)
                {
                    return Error.Error_TheCurrentMonitorObjectHasBeenAcquiredSoThatTheLockerCannotBeReentered;
                }
                return CacheAccessor.GetClient((storage) => CacheAccessor.AcquireLock(storage, this.GetKey(), out this.disposable, timeout));
            }
        }

        public virtual Error Exit()
        {
            lock (this)
            {
                if (this.disposable == null)
                {
                    return Error.Error_UnableToMonitorIsExitNotAnyTheFetchLockerObject;
                }
                Error error = CacheAccessor.Unlock(this.disposable);
                if (error == Error.Error_Success)
                {
                    this.disposable = null;
                }
                return error;
            }
        }

        public virtual Error Synchronize(Func<Error> critical, int timeout = 3)
        {
            if (critical == null)
            {
                return Error.Error_SeriousCodingErrorsHandingMustBeStrictlyValidAndDoNotAllowTheUseOfAnyNullForm;
            }
            return CacheAccessor.GetClient((storage) => CacheAccessor.AcquireLock(storage, this.GetKey(), critical, timeout));
        }
    }
}
