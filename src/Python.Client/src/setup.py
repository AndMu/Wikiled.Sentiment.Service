import codecs
import os
import re

import setuptools

with open("Readme.md", "r") as fh:
    long_description = fh.read()

with open('requirements.txt') as f:
    required = f.read().splitlines()

here = os.path.abspath(os.path.dirname(__file__))


def read(*parts):
    with codecs.open(os.path.join(here, *parts), 'r') as fp:
        return fp.read()


def find_version(*file_paths):
    version_file = read(*file_paths)
    version_match = re.search(r"^__version__ = ['\"]([^'\"]*)['\"]", version_file, re.M)
    if version_match:
        return version_match.group(1)
    raise RuntimeError("Unable to find version string.")


project_name = 'psenti'

setuptools.setup(
    name=project_name,
    version=find_version(project_name, "__init__.py"),
    python_requires='>3.6',
    author="Wikiled",    
    description="pSenti API",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/AndMu/Wikiled.Sentiment.Service",
    install_requires=required,
    packages=setuptools.find_packages(),
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
)
