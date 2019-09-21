using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace NetRpc
{
    public sealed class ContractInfo
    {
        private readonly Dictionary<MemberInfo, List<FaultExceptionAttribute>> _faultDic = new Dictionary<MemberInfo, List<FaultExceptionAttribute>>();

        public ContractInfo(Type type)
        {
            Type = type;

            var cDefines = type.GetCustomAttributes<FaultExceptionDefineAttribute>(true).ToList();
            var cFaults = type.GetCustomAttributes<FaultExceptionAttribute>(true).ToList();

            foreach (var m in type.GetInterfaceMethods())
            {
                var faults = m.GetCustomAttributes<FaultExceptionAttribute>(true).ToList();
                faults.AddRange(cFaults);

                foreach (var f in faults)
                {
                    var foundF = cDefines.FirstOrDefault(i => i.DetailType == f.DetailType);
                    if (foundF != null)
                    {
                        f.StatusCode = foundF.StatusCode;
                        f.ErrorCode = foundF.ErrorCode;
                        f.Summary = foundF.Summary;
                    }
                }

                _faultDic[m] = faults;
                Methods.Add(m);
            }
        }

        public Type Type { get; }

        public List<FaultExceptionAttribute> GetFaults(MethodInfo contractMethod)
        {
            return _faultDic[contractMethod];
        }

        public List<MethodInfo> Methods { get; } = new List<MethodInfo>();
    }

    public class Contract
    {
        private ContractInfo Info;

        private Type _contractType;

        public Type ContractType
        {
            get => _contractType;
            set
            {
                _contractType = value;
                Info = new ContractInfo(value);
            }
        }

        public List<FaultExceptionAttribute> GetFaults(MethodInfo contractMethod)
        {
            return Info.GetFaults(contractMethod);
        }

        public List<MethodInfo> Methods => Info.Methods;

        public Type InstanceType { get; set; }

        public Contract()
        {
        }

        public Contract(Type contractType, Type instanceType)
        {
            ContractType = contractType;
            InstanceType = instanceType;
        }
    }

    public sealed class Contract<TService, TImplementation> : Contract where TService : class
        where TImplementation : class, TService
    {
        public Contract()
        {
            ContractType = typeof(TService);
            InstanceType = typeof(TImplementation);
        }
    }
}