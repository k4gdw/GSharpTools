﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace pserv3.Services
{
    class RequestServiceRestart : ServiceStateRequest
    {
        private bool HasBeenAskedToStart;
        private ServiceStatus SS;

        #region ServiceStateRequest Members

        public ACCESS_MASK GetServiceAccessMask()
        {
            return ACCESS_MASK.SERVICE_STOP|ACCESS_MASK.SERVICE_START;
        }

        public bool Request(ServiceStatus ss)
        {
            SS = ss;
            HasBeenAskedToStart = false;
            return ss.Control(SC_CONTROL_CODE.SERVICE_CONTROL_STOP);
        }

        public bool HasSuccess(SC_RUNTIME_STATUS state)
        {
            if (SS == null)
                return false;

            if (HasBeenAskedToStart)
                return state == SC_RUNTIME_STATUS.SERVICE_RUNNING;

            if (state != SC_RUNTIME_STATUS.SERVICE_STOPPED)
                return false;

            Trace.TraceInformation("Restart asks for service to start...");
            if (!SS.Start())
                return false;
            
            HasBeenAskedToStart = true;
            return true;
        }

        public bool HasFailed(SC_RUNTIME_STATUS state)
        {
            return false;
        }

        #endregion
    }
}
