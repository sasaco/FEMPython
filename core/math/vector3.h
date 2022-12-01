#ifndef THREE_VECTOR3_H
#define THREE_VECTOR3_H

#include <three/common.h>
#include <three/console.h>

#include <three/constants.h>

#include <three/math/math.h>

#include <three/utils/memory.h>

#include <memory>

namespace three {

class THREE_DECL Vector3 {

public:

  union {
    struct {
      float x, y, z;
    };
    float xyz[3];
  };

  Vector3()
    : x( 0.f ), y( 0.f ), z( 0.f ) {}

  Vector3( float xIn, float yIn, float zIn )
    : x( xIn ), y( yIn ), z( zIn ) {}

  explicit Vector3( float value )
    : x( value ), y( value ), z( value ) {}

  explicit Vector3( float* values )
    : x( values[0] ), y( values[1] ), z( values[2] ) {}

  float& operator[]( const int i ) {
    return xyz[i];
  }

  const float operator[]( const int i ) const {
    return xyz[i];
  }

  inline Vector3& set( float xIn, float yIn, float zIn ) {

    x = xIn;
    y = yIn;
    z = zIn;

    return *this;

  }

  inline Vector3& setX( float xIn ) {

    x = xIn;

    return *this;

  }

  inline Vector3& setY( float yIn ) {

    y = yIn;

    return *this;

  }

  inline Vector3& setZ( float zIn ) {

    z = zIn;

    return *this;

  }

  inline Vector3& setComponent( int index, float value ) {

    switch ( index ) {

      case 0: x = value; break;
      case 1: y = value; break;
      case 2: z = value; break;
        default: break;

    }
    return *this;
  }

  inline float getComponent( int index ) {

    switch ( index ) {

      case 0: return x;
      case 1: return y;
      case 2: return z;
      default: return x;

    }

  }

  inline Vector3& copy( const Vector3& v ) {

    x = v.x;
    y = v.y;
    z = v.z;

    return *this;

  }

  Vector3& copy( const Vector4& v );

  inline Vector3& add( const Vector3& v ) {

    x += v.x;
    y += v.y;
    z += v.z;

    return *this;

  }

  inline Vector3& addScalar( float s ) {

    x += s;
    y += s;
    z += s;

    return *this;

  }

  inline Vector3& addVectors( const Vector3& a, const Vector3& b ) {

    x = a.x + b.x;
    y = a.y + b.y;
    z = a.z + b.z;

    return *this;

  }

  inline Vector3& sub( const Vector3& v ) {

    x -= v.x;
    y -= v.y;
    z -= v.z;

    return *this;

  }

  inline Vector3& subVectors( const Vector3& a, const Vector3& b ) {

    x = a.x - b.x;
    y = a.y - b.y;
    z = a.z - b.z;

    return *this;

  }

  inline Vector3& multiply( const Vector3& v ) {

    x *= v.x;
    y *= v.y;
    z *= v.z;

    return *this;

  }

  inline Vector3& multiplyScalar( float s ) {

    x *= s;
    y *= s;
    z *= s;

    return *this;

  }

  inline Vector3& multiplyVectors( const Vector3& a, const Vector3& b ) {

    x = a.x * b.x;
    y = a.y * b.y;
    z = a.z * b.z;

    return *this;

  }

  Vector3& applyMatrix3( const Matrix3& m );

  Vector3& applyMatrix4( const Matrix4& m );

  Vector3& applyProjection( const Matrix4& m );

  Vector3& applyQuaternion( const Quaternion& q );

  Vector3& transformDirection( const Matrix4& m );

  inline Vector3& divide( const Vector3& v ) {

    // @todo what's the desired/correct behavior?

    if( v.x != 0.f ) {

      x /= v.x;

    } else {

      x = 0.f;

    }

    if( v.y != 0.f ) {

      y /= v.y;

    } else {

      y = 0.f;

    }

    if( v.z != 0.f ) {

      z /= v.z;

    } else {

      z = 0.f;

    }

    return *this;

  }

  inline Vector3& divideScalar( float s ) {

    if ( s != 0.f ) {
      auto invScalar = 1.f / s;

      x *= invScalar;
      y *= invScalar;
      z *= invScalar;

    } else {

      set( 0.f, 0.f, 0.f );

    }

    return *this;

  }

  inline Vector3& min( const Vector3& v ) {

    if ( x > v.x ) {

      x = v.x;

    }

    if ( y > v.y ) {

      y = v.y;

    }

    if ( z > v.z ) {

      z = v.z;

    }

    return *this;

  }

  inline Vector3& max( const Vector3& v ) {

    if ( x < v.x ) {

      x = v.x;

    }

    if ( y < v.y ) {

      y = v.y;

    }

    if ( z < v.z ) {

      z = v.z;

    }

    return *this;

  }

  inline Vector3& clamp( const Vector3& min, const Vector3&  max ) {

    // This function assumes min < max, if this assumption isn't true it will not operate correctly

    if ( x < min.x ) {

      x = min.x;

    } else if ( x > max.x ) {

      x = max.x;

    }

    if ( y < min.y ) {

      y = min.y;

    } else if ( y > max.y ) {

      y = max.y;

    }

    if ( z < min.z ) {

      z = min.z;

    } else if ( z > max.z ) {

      z = max.z;

    }

    return *this;

  }

  inline Vector3& negate() {

    return multiplyScalar( -1.f );

  }

  inline float dot( const Vector3& v ) const {

    return (x * v.x) + (y * v.y) + (z * v.z);

  }

  inline float lengthSq() const {

    return (x * x) + (y * y) + (z * z);

  }

  inline float length() const {

    return Math::sqrt( lengthSq() );

  }

  inline float lengthManhattan() const {

    return Math::abs( x ) + Math::abs( y ) + Math::abs( z );

  }

  inline Vector3& normalize() {

    return divideScalar( length() );

  }

  inline Vector3& setLength( float l ) {

    return normalize().multiplyScalar( l );

  }

  inline Vector3& lerp( const Vector3& v, float alpha ) {

    x += ( v.x - x ) * alpha;
    y += ( v.y - y ) * alpha;
    z += ( v.z - z ) * alpha;

    return *this;

  }

  inline Vector3& cross( const Vector3& v) {

    auto x = this->x, y = this->y, z = this->z;

    this->x = y * v.z - z * v.y;
    this->y = z * v.x - x * v.z;
    this->z = x * v.y - y * v.x;

    return *this;

  }

  inline Vector3& crossVectors( const Vector3& a, const Vector3& b ) {

    auto ax = a.x, ay = a.y, az = a.z;
    auto bx = b.x, by = b.y, bz = b.z;

    this->x = ay * bz - az * by;
    this->y = az * bx - ax * bz;
    this->z = ax * by - ay * bx;

    return *this;

  }

  inline float angleTo( const Vector3& v ) {

    auto theta = dot( v ) / ( length() * v.length() );

    // clamp, to handle numerical problems

    return Math::acos( Math::clamp( theta, -1.f, 1.f ) );
  }

  inline float distanceTo( const Vector3& v ) const {

    return Math::sqrt( distanceToSquared( v ) );

  }

  inline float distanceToSquared( const Vector3& v ) const {

    auto dx = this->x - v.x;
    auto dy = this->y - v.y;
    auto dz = this->z - v.z;

    return (dx * dx) + (dy * dy) + (dz * dz);

  }

  Vector3& setFromMatrixPosition( const Matrix4& m );

  Vector3& setFromMatrixScale( const Matrix4& m );

  Vector3& setFromMatrixColumn( size_t index, const Matrix4& matrix );

  inline bool equals( const Vector3& v ) const {

    return v.x == x && v.y == y && v.z == z;

  }

  inline Vector3 clone() const {

    return Vector3(*this);

  }

  Vector3& applyEuler( const Euler& euler );

  Vector3& applyAxisAngle( const Vector3& axis, float angle );

  inline Vector3& projectOnVector( const Vector3& vector ) {

    auto v1 = Vector3();
    v1.copy( vector ).normalize();
    auto d = dot( v1 );

    return copy( v1 ).multiplyScalar( d );

  }

  inline Vector3& projectOnPlane( const Vector3& planeNormal ) {

    auto v1 = Vector3();
    v1.copy( *this ).projectOnVector( planeNormal );

    return sub( v1 );

  }

  inline Vector3& reflect( const Vector3& vector ) {

    auto v1 = Vector3();
    v1.copy( *this ).projectOnVector( vector ).multiplyScalar( 2 );

    return subVectors( v1, *this );

  }

};

static_assert( sizeof( Vector3 ) == sizeof( float ) * 3, "Invalid Vector3 storage size" );

inline Vector3 add( const Vector3& a, const Vector3& b ) {
  return Vector3().addVectors( a, b );
}

inline Vector3 sub( const Vector3& a, const Vector3& b ) {
  return Vector3().subVectors( a, b );
}

inline float dot( const Vector3& a, const Vector3& b ) {
  return a.dot( b );
}

inline Vector3 cross( const Vector3& a, const Vector3& b ) {
  return Vector3().crossVectors( a, b );
}



} // namespace three

#endif // THREE_VECTOR3_H