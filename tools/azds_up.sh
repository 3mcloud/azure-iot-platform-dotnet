if [ $1 == "usage" ] ; then
    echo "Usage: azds_up.sh <service_name>"
    echo "Usage: azds_up.sh -o <service_name>   ---   does not overwrite charts under azds directory"
    echo "Must be ran from repo root"
fi

if [ $1 == "-o" ] ; then
    servicePath=src/services/$2/WebService
else
    servicePath=src/services/$1/WebService
    mkdir $servicePath/azds
    cp -R charts/mmm-iot-service/* $servicePath/azds
    sed -i "" "/^\(name: \).*/s//\1$1/" $servicePath/azds/Chart.yaml
fi

cd $servicePath && azds up -d ; cd ../../../..
azds list-uris