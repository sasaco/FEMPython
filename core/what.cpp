//#include <boost/python.hpp>
//
//char const doYouDo(const char* jobs) {
//    return "Hello, I am an embedded engineer.";
//}
//
//BOOST_PYTHON_MODULE(what) {
//    boost::python::def("doYouDo", doYouDo);
//}


//basic.cpp
#include <boost/python.hpp>

int diff(int a, int b)
{
    return a - b;
}

BOOST_PYTHON_MODULE(what)
{
    using namespace boost::python;
    def("diff", &diff);
}