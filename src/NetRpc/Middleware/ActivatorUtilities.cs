using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NetRpc;

/// <summary>
/// Helper code for the various activator services.
/// </summary>
internal static class ActivatorUtilities
{
    private static readonly MethodInfo GetServiceInfo =
        GetMethodInfo<Func<IServiceProvider, Type, Type, bool, object>>((sp, t, r, c) => GetService(sp, t, r, c));

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="instanceType">The type to activate</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type instanceType</returns>
    public static object CreateInstance(IServiceProvider provider, Type instanceType, params object[] parameters)
    {
        var bestLength = -1;
        var seenPreferred = false;

        ConstructorMatcher? bestMatcher = null;

        if (!instanceType.GetTypeInfo().IsAbstract)
        {
            foreach (var constructor in instanceType
                         .GetTypeInfo()
                         .DeclaredConstructors
                         .Where(c => !c.IsStatic && c.IsPublic))
            {
                var matcher = new ConstructorMatcher(constructor);
                var isPreferred = constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false);
                var length = matcher.Match(parameters);

                if (isPreferred)
                {
                    if (seenPreferred)
                    {
                        ThrowMultipleCtorsMarkedWithAttributeException();
                    }

                    if (length == -1)
                    {
                        ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                    }
                }

                if (isPreferred || bestLength < length)
                {
                    bestLength = length;
                    bestMatcher = matcher;
                }

                seenPreferred |= isPreferred;
            }
        }

        if (bestMatcher == null)
        {
            var message =
                $"A suitable constructor for type '{instanceType}' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            throw new InvalidOperationException(message);
        }

        return bestMatcher.CreateInstance(provider);
    }

    /// <summary>
    /// Create a delegate that will instantiate a type with constructor arguments provided directly
    /// and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <param name="instanceType">The type to activate</param>
    /// <param name="argumentTypes">
    /// The types of objects, in order, that will be passed to the returned function as its second parameter
    /// </param>
    /// <returns>
    /// A factory that will instantiate instanceType using an <see cref="IServiceProvider"/>
    /// and an argument array containing objects matching the types defined in argumentTypes
    /// </returns>
    public static ObjectFactory CreateFactory(Type instanceType, Type[] argumentTypes)
    {
        FindApplicableConstructor(instanceType, argumentTypes, out var constructor, out var parameterMap);

        var provider = Expression.Parameter(typeof(IServiceProvider), "provider");
        var argumentArray = Expression.Parameter(typeof(object[]), "argumentArray");
        var factoryExpressionBody = BuildFactoryExpression(constructor!, parameterMap!, provider, argumentArray);

        var factoryLamda = Expression.Lambda<Func<IServiceProvider, object[], object>>(
            factoryExpressionBody, provider, argumentArray);

        var result = factoryLamda.Compile();
        return result.Invoke!;
    }

    /// <summary>
    /// Instantiate a type with constructor arguments provided directly and/or from an <see cref="IServiceProvider"/>.
    /// </summary>
    /// <typeparam name="T">The type to activate</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <param name="parameters">Constructor arguments not provided by the <paramref name="provider"/>.</param>
    /// <returns>An activated object of type T</returns>
    public static T CreateInstance<T>(IServiceProvider provider, params object[] parameters)
    {
        return (T)CreateInstance(provider, typeof(T), parameters);
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <typeparam name="T">The type of the service</typeparam>
    /// <param name="provider">The service provider used to resolve dependencies</param>
    /// <returns>The resolved service or created instance</returns>
    public static T GetServiceOrCreateInstance<T>(IServiceProvider provider)
    {
        return (T)GetServiceOrCreateInstance(provider, typeof(T));
    }

    /// <summary>
    /// Retrieve an instance of the given type from the service provider. If one is not found then instantiate it directly.
    /// </summary>
    /// <param name="provider">The service provider</param>
    /// <param name="type">The type of the service</param>
    /// <returns>The resolved service or created instance</returns>
    public static object GetServiceOrCreateInstance(IServiceProvider provider, Type type)
    {
        return provider.GetService(type) ?? CreateInstance(provider, type);
    }

    private static MethodInfo GetMethodInfo<T>(Expression<T> expr)
    {
        var mc = (MethodCallExpression)expr.Body;
        return mc.Method;
    }

    private static object GetService(IServiceProvider sp, Type type, Type requiredBy, bool isDefaultParameterRequired)
    {
        var service = sp.GetService(type);
        if (service == null && !isDefaultParameterRequired)
        {
            var message = $"Unable to resolve service for type '{type}' while attempting to activate '{requiredBy}'.";
            throw new InvalidOperationException(message);
        }

        return service!;
    }

    private static Expression BuildFactoryExpression(
        ConstructorInfo constructor,
        int?[] parameterMap,
        Expression serviceProvider,
        Expression factoryArgumentArray)
    {
        var constructorParameters = constructor.GetParameters();
        var constructorArguments = new Expression[constructorParameters.Length];

        for (var i = 0; i < constructorParameters.Length; i++)
        {
            var constructorParameter = constructorParameters[i];
            var parameterType = constructorParameter.ParameterType;
            var hasDefaultValue = ParameterDefaultValue.TryGetDefaultValue(constructorParameter, out var defaultValue);

            if (parameterMap[i] != null)
            {
                constructorArguments[i] = Expression.ArrayAccess(factoryArgumentArray, Expression.Constant(parameterMap[i]));
            }
            else
            {
                var parameterTypeExpression = new[]
                {
                    serviceProvider,
                    Expression.Constant(parameterType, typeof(Type)),
                    Expression.Constant(constructor.DeclaringType, typeof(Type)),
                    Expression.Constant(hasDefaultValue)
                };
                constructorArguments[i] = Expression.Call(GetServiceInfo, parameterTypeExpression);
            }

            // Support optional constructor arguments by passing in the default value
            // when the argument would otherwise be null.
            if (hasDefaultValue)
            {
                var defaultValueExpression = Expression.Constant(defaultValue);
                constructorArguments[i] = Expression.Coalesce(constructorArguments[i], defaultValueExpression);
            }

            constructorArguments[i] = Expression.Convert(constructorArguments[i], parameterType);
        }

        return Expression.New(constructor, constructorArguments);
    }

    private static void FindApplicableConstructor(
        Type instanceType,
        Type[] argumentTypes,
        out ConstructorInfo? matchingConstructor,
        out int?[]? parameterMap)
    {
        matchingConstructor = null;
        parameterMap = null;

        if (!TryFindPreferredConstructor(instanceType, argumentTypes, ref matchingConstructor!, ref parameterMap!) &&
            !TryFindMatchingConstructor(instanceType, argumentTypes, ref matchingConstructor, ref parameterMap))
        {
            var message =
                $"A suitable constructor for type '{instanceType}' could not be located. Ensure the type is concrete and services are registered for all parameters of a public constructor.";
            throw new InvalidOperationException(message);
        }
    }

    // Tries to find constructor based on provided argument types
    private static bool TryFindMatchingConstructor(
        Type instanceType,
        Type[] argumentTypes,
        ref ConstructorInfo matchingConstructor,
        ref int?[] parameterMap)
    {
        foreach (var constructor in instanceType.GetTypeInfo().DeclaredConstructors)
        {
            if (constructor.IsStatic || !constructor.IsPublic)
            {
                continue;
            }

            if (TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out var tempParameterMap))
            {
                if (matchingConstructor != null)
                {
                    throw new InvalidOperationException(
                        $"Multiple constructors accepting all given argument types have been found in type '{instanceType}'. There should only be one applicable constructor.");
                }

                matchingConstructor = constructor;
                parameterMap = tempParameterMap;
            }
        }

        return matchingConstructor != null;
    }

    // Tries to find constructor marked with ActivatorUtilitiesConstructorAttribute
    private static bool TryFindPreferredConstructor(
        Type instanceType,
        Type[] argumentTypes,
        ref ConstructorInfo matchingConstructor,
        ref int?[] parameterMap)
    {
        var seenPreferred = false;
        foreach (var constructor in instanceType.GetTypeInfo().DeclaredConstructors)
        {
            if (constructor.IsStatic || !constructor.IsPublic)
            {
                continue;
            }

            if (constructor.IsDefined(typeof(ActivatorUtilitiesConstructorAttribute), false))
            {
                if (seenPreferred)
                {
                    ThrowMultipleCtorsMarkedWithAttributeException();
                }

                if (!TryCreateParameterMap(constructor.GetParameters(), argumentTypes, out var tempParameterMap))
                {
                    ThrowMarkedCtorDoesNotTakeAllProvidedArguments();
                }

                matchingConstructor = constructor;
                parameterMap = tempParameterMap;
                seenPreferred = true;
            }
        }

        return matchingConstructor != null;
    }

    // Creates an injective parameterMap from givenParameterTypes to assignable constructorParameters.
    // Returns true if each given parameter type is assignable to a unique; otherwise, false.
    private static bool TryCreateParameterMap(ParameterInfo[] constructorParameters, Type[] argumentTypes, out int?[] parameterMap)
    {
        parameterMap = new int?[constructorParameters.Length];

        for (var i = 0; i < argumentTypes.Length; i++)
        {
            var foundMatch = false;
            var givenParameter = argumentTypes[i].GetTypeInfo();

            for (var j = 0; j < constructorParameters.Length; j++)
            {
                if (parameterMap[j] != null)
                {
                    // This ctor parameter has already been matched
                    continue;
                }

                if (constructorParameters[j].ParameterType.GetTypeInfo().IsAssignableFrom(givenParameter))
                {
                    foundMatch = true;
                    parameterMap[j] = i;
                    break;
                }
            }

            if (!foundMatch)
            {
                return false;
            }
        }

        return true;
    }

    private class ConstructorMatcher
    {
        private readonly ConstructorInfo _constructor;
        private readonly ParameterInfo[] _parameters;
        private readonly object[] _parameterValues;
        private readonly bool[] _parameterValuesSet;

        public ConstructorMatcher(ConstructorInfo constructor)
        {
            _constructor = constructor;
            _parameters = _constructor.GetParameters();
            _parameterValuesSet = new bool[_parameters.Length];
            _parameterValues = new object[_parameters.Length];
        }

        public int Match(object[] givenParameters)
        {
            var applyIndexStart = 0;
            var applyExactLength = 0;
            for (var givenIndex = 0; givenIndex != givenParameters.Length; givenIndex++)
            {
                var givenType = givenParameters[givenIndex]?.GetType().GetTypeInfo();
                var givenMatched = false;

                for (var applyIndex = applyIndexStart; givenMatched == false && applyIndex != _parameters.Length; ++applyIndex)
                {
                    if (_parameterValuesSet[applyIndex] == false &&
                        _parameters[applyIndex].ParameterType.GetTypeInfo().IsAssignableFrom(givenType))
                    {
                        givenMatched = true;
                        _parameterValuesSet[applyIndex] = true;
                        _parameterValues[applyIndex] = givenParameters[givenIndex];
                        if (applyIndexStart == applyIndex)
                        {
                            applyIndexStart++;
                            if (applyIndex == givenIndex)
                            {
                                applyExactLength = applyIndex;
                            }
                        }
                    }
                }

                if (givenMatched == false)
                {
                    return -1;
                }
            }

            return applyExactLength;
        }

        public object CreateInstance(IServiceProvider provider)
        {
            for (var index = 0; index != _parameters.Length; index++)
            {
                if (_parameterValuesSet[index] == false)
                {
                    var value = provider.GetService(_parameters[index].ParameterType);
                    if (value == null)
                    {
                        if (!ParameterDefaultValue.TryGetDefaultValue(_parameters[index], out var defaultValue))
                        {
                            throw new InvalidOperationException(
                                $"Unable to resolve service for type '{_parameters[index].ParameterType}' while attempting to activate '{_constructor.DeclaringType}'.");
                        }

                        _parameterValues[index] = defaultValue!;
                    }
                    else
                    {
                        _parameterValues[index] = value;
                    }
                }
            }

            return _constructor.Invoke(BindingFlags.DoNotWrapExceptions, null, _parameterValues, null);
        }
    }

    private static void ThrowMultipleCtorsMarkedWithAttributeException()
    {
        throw new InvalidOperationException($"Multiple constructors were marked with {nameof(ActivatorUtilitiesConstructorAttribute)}.");
    }

    private static void ThrowMarkedCtorDoesNotTakeAllProvidedArguments()
    {
        throw new InvalidOperationException(
            $"Constructor marked with {nameof(ActivatorUtilitiesConstructorAttribute)} does not accept all given argument types.");
    }
}

internal class ParameterDefaultValue
{
    private static readonly Type _nullable = typeof(Nullable<>);

    public static bool TryGetDefaultValue(ParameterInfo parameter, [NotNullWhen(true)] out object? defaultValue)
    {
        bool hasDefaultValue;
        var tryToGetDefaultValue = true;
        defaultValue = null;

        try
        {
            hasDefaultValue = parameter.HasDefaultValue;
        }
        catch (FormatException) when (parameter.ParameterType == typeof(DateTime))
        {
            // Workaround for https://github.com/dotnet/corefx/issues/12338
            // If HasDefaultValue throws FormatException for DateTime
            // we expect it to have default value
            hasDefaultValue = true;
            tryToGetDefaultValue = false;
        }

        if (hasDefaultValue)
        {
            if (tryToGetDefaultValue)
            {
                defaultValue = parameter.DefaultValue;
            }

            // Workaround for https://github.com/dotnet/corefx/issues/11797
            if (defaultValue == null && parameter.ParameterType.IsValueType)
            {
                defaultValue = Activator.CreateInstance(parameter.ParameterType);
            }

            // Handle nullable enums
            if (defaultValue != null &&
                parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == _nullable
               )
            {
                var underlyingType = Nullable.GetUnderlyingType(parameter.ParameterType);
                if (underlyingType != null && underlyingType.IsEnum)
                {
                    defaultValue = Enum.ToObject(underlyingType, defaultValue);
                }
            }
        }

        return hasDefaultValue;
    }
}