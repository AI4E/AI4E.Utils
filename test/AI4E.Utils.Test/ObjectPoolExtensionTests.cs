using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.ObjectPool;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AI4E.Utils
{
    [TestClass]
    public class ObjectPoolExtensionTests
    {
        [TestMethod]
        public void DiposingDefaultPooledObjectReturnerShallNotThrowTest()
        {
            var defaultReturner = default(PooledObjectReturner<object>);
            defaultReturner.Dispose();
        }

        [TestMethod]
        public void PooledObjectReturnerDisposeTest()
        {
            var poolMock = new ObjectPoolMock();
            var pooledObject = new object();
            var objectReturner = new PooledObjectReturner<object>(poolMock, pooledObject);
            objectReturner.Dispose();

            Assert.AreSame(pooledObject, poolMock._objects.Single());
        }

        [TestMethod]
        public void PooledObjectReturnerDoubleDisposeTest()
        {
            var poolMock = new ObjectPoolMock();
            var pooledObject = new object();
            var objectReturner = new PooledObjectReturner<object>(poolMock, pooledObject);
            objectReturner.Dispose();
            objectReturner.Dispose();

            Assert.AreSame(pooledObject, poolMock._objects.Single());
        }

        [TestMethod]
        public void PooledObjectReturnerCopyTest()
        {
            var poolMock = new ObjectPoolMock();
            var pooledObject = new object();
            var objectReturner = new PooledObjectReturner<object>(poolMock, pooledObject);
            Dispose(objectReturner);
            objectReturner.Dispose();

            Assert.AreSame(pooledObject, poolMock._objects.Single());
        }

        private void Dispose(PooledObjectReturner<object> objectReturner) // Copy by value
        {
            objectReturner.Dispose();
        }

        [TestMethod]
        public void ObjectPoolGetExtensionTest()
        {
            var poolMock = new ObjectPoolMock();
            var pooledObject = new object();
            poolMock._objects.Add(pooledObject);
            var objectReturner = poolMock.Get(out var rentedObject);
            objectReturner.Dispose();

            Assert.AreSame(pooledObject, rentedObject);
            Assert.AreSame(pooledObject, poolMock._objects.Single());
        }
    }

    public class ObjectPoolMock : ObjectPool<object>
    {
        public List<object> _objects = new List<object>();

        public override object Get()
        {
            var result = _objects.Last();
            _objects.RemoveAt(_objects.Count - 1);
            return result;
        }

        public override void Return(object obj)
        {
            _objects.Add(obj);
        }
    }
}
