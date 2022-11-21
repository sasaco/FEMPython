
class Matrix3 {
public:

  union {
    float elements[9];
    float te[9];
  };

  Matrix3();
  Matrix3( float n11, float n12, float n13,
                      float n21, float n22, float n23,
                      float n31, float n32, float n33 );
  Matrix3( const Matrix3& other );
  Matrix3& operator= ( const Matrix3& other );

  Matrix3& getInverse( const Matrix4& m );
  Matrix3& transpose();

  const Matrix3& transposeIntoArray( float* r ) const;

  Matrix3& set( float n11, float n12, float n13,
                           float n21, float n22, float n23,
                           float n31, float n32, float n33 );
  Matrix3& identity();

  Matrix3& copy( const Matrix3& m );
  Matrix3& copy( const Matrix4& m );
  Matrix3 clone();

};
