import setuptools

with open("Readme.md", "r") as fh:
    long_description = fh.read()


setuptools.setup(
    name="pysenti",
    version="0.1.0",
    python_requires='>3.6',
    author="Wikiled",    
    description="pySenti server API",
    long_description=long_description,
    long_description_content_type="text/markdown",
    url="https://github.com/AndMu/Wikiled.Sentiment.Service",
    packages=setuptools.find_packages(),
    classifiers=[
        "Programming Language :: Python :: 3",
        "License :: OSI Approved :: MIT License",
        "Operating System :: OS Independent",
    ],
)
