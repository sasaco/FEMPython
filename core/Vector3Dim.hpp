#pragma once

#include <cmath>

class Vector3Dim {
public:

    union {
        struct { 
            double x, y, z; 
        };
        double xyz[3];
    };

    Vector3Dim() : x(0), y(0), z(0) { }
    Vector3Dim(double xIn, double yIn, double zIn) : x(xIn), y(yIn), z(zIn){ }
    explicit Vector3Dim(double value) : x(value), y(value), z(value) { }
    explicit Vector3Dim(double* values) : x(values[0]), y(values[1]), z(values[2]) { }

    double& operator[](const int i) { return xyz[i]; }
    const double operator[](const int i) const { return xyz[i]; }

    /// <summary>
    /// FElementで使用
    /// </summary>
    /// <param name="xIn"></param>
    /// <param name="yIn"></param>
    /// <param name="zIn"></param>
    /// <returns></returns>
    Vector3Dim& set(double xIn, double yIn, double zIn) {
        x = xIn;
        y = yIn;
        z = zIn;
        return *this;
    }

    Vector3Dim& copy(const Vector3Dim& v) {
        x = v.x;
        y = v.y;
        z = v.z;
        return *this;
    }

    Vector3Dim& add(const Vector3Dim& v) {
        x += v.x;
        y += v.y;
        z += v.z;
        return *this;
    }

    Vector3Dim& add(const Vector3Dim& a, const Vector3Dim& b) {
        x = a.x + b.x;
        y = a.y + b.y;
        z = a.z + b.z;
        return *this;
    }

    /// <summary>
    /// FElementで使用
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    Vector3Dim& sub(const Vector3Dim& v) {
        x -= v.x;
        y -= v.y;
        z -= v.z;
        return *this;
    }

    Vector3Dim& multiplyScalar(double s) {
        x *= s;
        y *= s;
        z *= s;
        return *this;
    }

    Vector3Dim& divideScalar(double s) {
        if (s != 0.f) {
            x /= s;
            y /= s;
            z /= s;
        }
        else {
            set(0, 0, 0);
        }
        return *this;
    }

    Vector3Dim& negate() {
        return multiplyScalar(-1.f);
    }

    /// <summary>
    /// FElementで使用
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    double dot(const Vector3Dim& v) const {
        return x * v.x + y * v.y + z * v.z;
    }

    double lengthSq() const {
        return x * x + y * y + z * z;
    }

    double length() const {
        return sqrt(lengthSq());
    }

    double lengthManhattan() const {
        return abs(x) + abs(y) + abs(z);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    Vector3Dim& normalize() {
        return divideScalar(length());
    }


    Vector3Dim& crossVectors(const Vector3Dim& a, const Vector3Dim& b) {

        auto ax = a.x, ay = a.y, az = a.z;
        auto bx = b.x, by = b.y, bz = b.z;

        this->x = ay * bz - az * by;
        this->y = az * bx - ax * bz;
        this->z = ax * by - ay * bx;

        return *this;

    }



    double distanceTo(const Vector3Dim& v) const {
        return sqrt(distanceToSquared(v));
    }

    double distanceToSquared(const Vector3Dim& v) const {
        return Vector3Dim().sub(*this, v).lengthSq();
    }

    Vector3Dim& sub(const Vector3Dim& a, const Vector3Dim& b) {
        x = a.x - b.x;
        y = a.y - b.y;
        z = a.z - b.z;
        return *this;
    }

    Vector3Dim& setLength(double l) {
        return normalize().multiplyScalar(l);
    }

    Vector3Dim& lerpSelf(const Vector3Dim& v, double alpha) {
        x += (v.x - x) * alpha;
        y += (v.y - y) * alpha;
        z += (v.z - z) * alpha;
        return *this;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="v"></param>
    /// <returns></returns>
    inline Vector3Dim& cross(const Vector3Dim& v) {

        auto x = this->x, y = this->y, z = this->z;

        this->x = y * v.z - z * v.y;
        this->y = z * v.x - x * v.z;
        this->z = x * v.y - y * v.x;

        return *this;

    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    inline Vector3Dim& subVectors(const Vector3Dim& a, const Vector3Dim& b) {

        x = a.x - b.x;
        y = a.y - b.y;
        z = a.z - b.z;

        return *this;

    }


    bool equals(const Vector3Dim& v) {
        return ((v.x == x) && (v.y == y) && (v.z == z));
    }

    bool isZero() {
        return (lengthSq() < 0.0001f /* almostZero */);
    }

    Vector3Dim clone() const {
        return *this;
    }
};

static_assert(sizeof(Vector3Dim) == sizeof(double) * 3, "Invalid Vector3Dim storage size");

inline Vector3Dim add(const Vector3Dim& a, const Vector3Dim& b) {
    return Vector3Dim().add(a, b);
}

inline Vector3Dim sub(const Vector3Dim& a, const Vector3Dim& b) {
    return Vector3Dim().sub(a, b);
}

inline double dot(const Vector3Dim& a, const Vector3Dim& b) {
    return a.dot(b);
}

