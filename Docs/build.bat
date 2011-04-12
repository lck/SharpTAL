ECHO OFF

REM -------------------------
REM Requirements installation:
REM   1. Download and install [Python] from http://www.python.org/ftp/python/2.7/python-2.7.msi
REM   2. Download [Sphinx] package from http://pypi.python.org/packages/source/S/Sphinx/Sphinx-1.0.3.tar.gz
REM   3. Unpack [Sphinx-1.0.3.tar.gz]
REM   4. Install [Sphinx] package with command [c:\Python26\python.exe <SPHINX_FOLDER>\setup.py install]
REM -------------------------

c:\Python27\Scripts\sphinx-build.exe -b html . _build
