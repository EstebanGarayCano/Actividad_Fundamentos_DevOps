pipeline {
    agent any

    environment {
        DOCKERHUB_REPO = 'estebangaraycano/ecomify-customers'
        IMAGE_TAG = "${BUILD_NUMBER}"
        DOCKERFILE_PATH = 'EcomifyCustomers/Dockerfile'
        BUILD_CONTEXT = 'EcomifyCustomers'
    }

    stages {
        stage('Clonar repositorio') {
            steps {
                checkout scm
            }
        }

        stage('Verificar archivos del proyecto') {
            steps {
                sh '''
                    echo "Listando archivos del repositorio..."
                    ls -la
                    echo "Verificando Dockerfile..."
                    ls -la EcomifyCustomers
                '''
            }
        }

        stage('Construir imagen Docker') {
            steps {
                sh '''
                    echo "Construyendo imagen Docker..."
                    docker build \
                    -t $DOCKERHUB_REPO:$IMAGE_TAG \
                    -t $DOCKERHUB_REPO:latest \
                    -f $DOCKERFILE_PATH \
                    $BUILD_CONTEXT
                '''
            }
        }

        stage('Autenticarse en DockerHub') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'dockerhub-credentials',
                    usernameVariable: 'DOCKERHUB_USER',
                    passwordVariable: 'DOCKERHUB_TOKEN'
                )]) {
                    sh '''
                        echo "Autenticando en DockerHub..."
                        echo "$DOCKERHUB_TOKEN" | docker login -u "$DOCKERHUB_USER" --password-stdin
                    '''
                }
            }
        }

        stage('Publicar imagen en DockerHub') {
            steps {
                sh '''
                    echo "Publicando imagen en DockerHub..."
                    docker push $DOCKERHUB_REPO:$IMAGE_TAG
                    docker push $DOCKERHUB_REPO:latest
                '''
            }
        }
    }

    post {
        success {
            echo 'Pipeline CD ejecutado correctamente. Imagen publicada en DockerHub.'
        }

        failure {
            echo 'El pipeline CD falló. Revisar el Console Output de Jenkins.'
        }

        always {
            sh '''
                docker logout || true
            '''
        }
    }
}